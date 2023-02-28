#include "Diffraction.cuh"

using namespace glm;

__device__ struct RNG_State {
    uint64_t x, w1, s;
};

__device__ unsigned msws(RNG_State& rng) {
    rng.x *= rng.x;
    rng.x += (rng.w1 += rng.s);
    return unsigned(rng.x = (rng.x >> 32u) | (rng.x << 32u));

}
__device__ void init_msws(uint64_t seed, RNG_State& rng) {
    rng.x = 0u; rng.w1 = 0u;
    rng.s = (((uint64_t(1890726812u) << 32u) | seed) << 1u) | uint64_t(1u);

    msws(rng); msws(rng);
}

#define RandNext(rng) msws(rng)
#define RandNext2(rng) uvec2(msws(rng), msws(rng))
#define RandNext3(rng) uvec3(RandNext2(rng), msws(rng))
#define RandNext4(rng) uvec4(RandNext3(rng), msws(rng))

#define RandNextF(rng) (float(RandNext(rng) & 0x00ffffffu) / float(0x00ffffff))
#define RandNext2F(rng) (vec2(RandNext2(rng) & 0x00ffffffu) / float(0x00ffffff))
#define RandNext3F(rng) (vec3(RandNext3(rng) & 0x00ffffffu) / float(0x00ffffff))
#define RandNext4F(rng) (vec4(RandNext4(rng) & 0x00ffffffu) / float(0x00ffffff))

__constant__ float pi = 3.14159;
__constant__ float tau = 6.28318;
__constant__ float phi = 1.61803399;

__device__ mat2 Rotate(float a) {
    float s = sin(a);
    float c = cos(a);
    return mat2(c, -s, s, c);
}

__device__ vec2 BokehShape(RNG_State& rng, int bladeCount, float radius) {
    const int blades = bladeCount;

    vec2 uv = RandNext2F(rng);

    vec2 axis;
    const float angle = radians(360.0f) / float(blades);

    uv.x *= float(blades);
    float blade = floor(uv.x);
    uv.x = fract(uv.x);

    mat2 rot = Rotate(blade * angle);

    axis = rot * vec2(cos(angle / 2.0), sin(angle / 2.0) * (uv.x * 2.0 - 1.0));
    axis *= 1.0 - pow(1.0 - sqrt(uv.y), 1.0);

    return axis * radius;
}

__device__ vec2 SampleCircle(RNG_State& rngState, float radius) {
    vec2 rng = RandNext2F(rngState);
    float r = radius * sqrt(rng.x);
    float t = 2.0 * pi * rng.y;

    return r * vec2(cos(t), sin(t));
}

__device__ float absSquared(thrust::complex<float> value) {
    return value.real() * value.real() + value.imag() * value.imag();
}

__device__ float Hypot(vec2 position, float dist) {
    return sqrt(pow(position.x, 2.0) + pow(position.y, 2.0) + pow(dist, 2.0));
}

__device__ float Aperture(vec2 uv, float radius, int blades) {
    float r = 0.0;
    for (int i = 0; i < blades; ++i) {
        float angle = 2.0 * pi * (float(i) / float(blades));

        mat2 rot = Rotate(float(blades) * angle + radians(0.0));

        vec2 axis = rot * vec2(cos(angle), sin(angle));

        r = max(r, dot(axis, uv));
    }

    return float(r < (radius * 1e-6));
}

__device__ vec2 IndexToDistance(float i, float j, int N) {
    float x = (float(i) - float(N / 2)) / (float(N));
    float y = (float(N / 2) - float(j)) / (float(N));

    return vec2(x, y);
}
__device__ vec2 IndexToDistance(int i, int j, int N) {
    float x = (float(i) - float(N / 2)) / (float(N));
    float y = (float(N / 2) - float(j)) / (float(N));

    return vec2(x, y);
}

//#define NOT_AIRY

__device__ float BesselJ(float x) {
    float xx = x * x, a = 1. + .12138 * xx;
    return (sqrt(a) * (46.68634 + 5.82514 * xx) * sin(x)
        - x * (17.83632 + 2.02948 * xx) * cos(x)
        ) / ((57.70003 + 17.49211 * xx) * pow(a, 3. / 4.));
}

__device__ float AiryDisk(float sinTheta, float k) {
    float radius = 5.0;
    float airy = pow((2.0 * BesselJ(k * radius * sinTheta)) / (k * radius * sinTheta), 2.0f);
    if (isinf(airy)) {
        airy = 0.0;
    }
    if (isnan(airy)) {
        airy = 0.0;
    }
    return airy;
}

__device__ float Plancks(float t, float lambda) {
    const float h = 6.62607015e-16;
    const float c = 2.9e17;
    const float k = 1.38e-5;

    float p1 = 2.0 * h * pow(c, 2.0) * pow(lambda, -5.0);
    float p2 = exp((h * c) / (lambda * k * t)) - 1.0;

    return p1 / p2;
}

__global__ void DiffractionIntegral(float* diff, int wavelengthIndex, DiffractionSettings settings) {
    int globalThreadIndex = blockIdx.x * blockDim.x + threadIdx.x;
    int x = globalThreadIndex % settings.size;
    int y = globalThreadIndex / settings.size;

    unsigned s = unsigned(x * settings.size + y) * 720720u + unsigned(wavelengthIndex);

    RNG_State rng;
    init_msws(uint32_t(s), rng);

    //Everything is in micrometers
    float scale = settings.scale;
    float radius = settings.radius;
    float dist = settings.dist;

    //ensure scaleWeight + radiusWeight + distanceWeight == 1.0.
    float average = dot(
        vec3(scale, radius, dist),
        vec3(0.0f, 0.4f, 0.6f)
    );
    float deviation = sqrt(
        dot(
            pow(vec3(scale, radius, dist) - average, vec3(2.0f)),
            vec3(0.0f, 0.4f, 0.6f)
        )
    );

    float wavelength = ((441.0f * (float(wavelengthIndex) / (settings.wavelengthCount - 1))) + 390.0f) * 1e-3f;
    float k = 2.0f * pi / wavelength;

    float angle = pi / float(settings.bladeCount);
    float sinAngle = sin(angle);
    float cosAngle = cos(angle);
    float blades = float(settings.bladeCount);

    int steps = int(deviation * average * 20.0f * settings.quality);

    thrust::complex<float> integral = thrust::complex<float>(0.0f, 0.0f);
    for (int i = 0; i < steps; ++i) {
        vec2 uv = scale * ((vec2(x, y) / vec2(settings.size, settings.size)) - 0.5f);
        vec2 rngUV = BokehShape(rng, blades, radius);
        if (blades > 30) {
            rngUV = SampleCircle(rng, radius);
        }

        float r = length(vec3(uv - rngUV, dist));
        float rk = r * k;

        thrust::complex<float> term = thrust::complex<float>(cos(rk), sin(rk)) * (dist / (r * r));

        if (isnan(term.real())) {
            term = thrust::complex<float>(0.0f, 0.0f);
        }
        if (isnan(term.imag())) {
            term = thrust::complex<float>(0.0f, 0.0f);
        }
        if (isinf(term.real())) {
            term = thrust::complex<float>(1.0f, 0.0f);
        }
        if (isinf(term.imag())) {
            term = thrust::complex<float>(0.0f, 1.0f);
        }

        thrust::complex<float> lensPhaseDelay = 1.0f;

        integral += term * lensPhaseDelay;
    }
    integral /= steps;

    integral *= thrust::complex<float>(1.0f, 1.0f / wavelength);

    diff[x + y * settings.size] = absSquared(integral) * Plancks(6504.0f, wavelength * 1e3f);
}