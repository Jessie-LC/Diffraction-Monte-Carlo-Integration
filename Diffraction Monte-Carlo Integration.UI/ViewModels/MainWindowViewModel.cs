using Diffraction_Monte_Carlo_Integration.UI.Internal;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Diffraction_Monte_Carlo_Integration.UI.ViewModels
{
    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isRunning;

        public bool IsRunning {
            get => _isRunning;
            private set {
                _isRunning = value;
                OnPropertyChanged();
            }
        }

        public bool SquareScale {get; set;}
        public int TextureSize {get; set;}
        public float Quality {get; set;}
        public float Radius {get; set;}
        public float Scale {get; set;}
        public float Distance {get; set;}


        public MainWindowViewModel()
        {
            SquareScale = false;
            TextureSize = 256;
            Quality = 1.0f;
            Radius = 2.0f;
            Scale = 10.0f;
            Distance = 10.0f;
        }

        public async Task RunAsync(CancellationToken token = default)
        {
            IsRunning = true;

            var result = await Task.Run(() => DMCIWrapper.ComputeDiffractionImageExport(SquareScale, TextureSize, Quality, Radius, Scale, Distance), token);
            if (result != 0) throw new ApplicationException("Failed to generate image! An internal exception has occurred.");
            //await Task.Delay(3_000, token);

            IsRunning = false;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
