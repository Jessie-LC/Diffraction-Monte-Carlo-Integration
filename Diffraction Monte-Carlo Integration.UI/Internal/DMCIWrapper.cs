using System.Runtime.InteropServices;

namespace Diffraction_Monte_Carlo_Integration.UI.Internal
{
    internal class DMCIWrapper
    {
        [DllImport(@"Diffraction Monte-Carlo Integration.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int ComputeDiffractionImageExport(int size, float quality, float radius, float scale, float dist);
    }
}
