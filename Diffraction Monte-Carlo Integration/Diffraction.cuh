#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include "thrust/complex.h"
#include <iostream>
#include <cmath>
#include <chrono>
#include <fstream>
#include <cstddef>
#include <thread>
#include <atomic>

#include "glm/glm.hpp"

struct DiffractionSettings {
	bool squareScale;
	int size;
	int bladeCount;
	int wavelengthCount; //Not an actual diffraction kernel setting, just here for ease of use in the kernel

	float quality;
	float radius;
	float scale;
	float dist;
};

__global__ void DiffractionIntegral(float* diff, int wavelength, DiffractionSettings settings);