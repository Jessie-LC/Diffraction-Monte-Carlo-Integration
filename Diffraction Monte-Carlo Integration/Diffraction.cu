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
    vec2 m;
    m.x = sin(a);
    m.y = cos(a);
    return mat2(m.y, -m.x, m.x, m.y);
}

__device__ vec2 BokehShape(RNG_State& rng) {
    const int blades = 3;

    vec2 uv = RandNext2F(rng);

    vec2 axis;
    const float angle = radians(360.0f) / float(blades);

    uv.x *= float(blades);
    float blade = floor(uv.x);
    uv.x = fract(uv.x);

    mat2 rot = Rotate(blade * angle);

    axis = rot * vec2(cos(angle / 2.0), sin(angle / 2.0) * (uv.x * 2.0 - 1.0));
    axis *= 1.0 - pow(1.0 - sqrt(uv.y), 1.0);

    return axis;
}

__global__ void DiffractionIntegral(thrust::complex<float>* diff, int wavelengthIndex, DiffractionSettings settings) {
    int globalThreadIndex = blockIdx.x * blockDim.x + threadIdx.x;
    int x = globalThreadIndex % settings.size;
    int y = globalThreadIndex / settings.size;

    unsigned s = unsigned(x * settings.size + y) * 720720u;

    RNG_State rng;
    init_msws(uint32_t(s), rng);

    //Everything is in micrometers
    float scale = settings.scale;
    float radius = settings.radius;
    float dist = settings.dist;

    float wavelength = ((441.0f * (float(wavelengthIndex) / (wavelengthCount - 1))) + 390.0f) * 1e-3f;

    int steps = int((pow(radius, 2.0f) * pow(dist, 2.0f)) * settings.quality);
    thrust::complex<float> integral = thrust::complex<float>(0.0f, 0.0f);
    for (int i = 0; i < steps; ++i) {

        vec2 uv = scale * ((vec2(x, y) / vec2(settings.size, settings.size)) - 0.5f);
        vec2 rngUV = BokehShape(rng) * radius;

        float k = 2.0f * pi / wavelength;
        float r = length(vec3(uv, dist) - vec3(rngUV, 0.0f));

        thrust::complex<float> term = (thrust::exp(thrust::complex<float>(0.0f, 1.0f) * (r * k)) / r) * (dist / r);

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

        integral += term * (1.0f / steps);
    }

    integral = (thrust::complex<float>(1.0f, 0.0f) / (thrust::complex<float>(0.0f, 1.0f) * wavelength)) * integral;

    diff[x + y * settings.size] = integral;
}