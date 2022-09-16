#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include "thrust/complex.h"
#include <iostream>
#include <cmath>
#include <chrono>
#include <fstream>
#include <cstddef>

#include "glm/glm.hpp"

#include "Spectra.cuh"

const int SIZE = 512;
const int wavelengthCount = 50;

__global__ void DiffractionIntegral(thrust::complex<float>* diff, int wavelength);