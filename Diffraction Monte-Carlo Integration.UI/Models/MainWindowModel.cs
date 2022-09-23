using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Diffraction_Monte_Carlo_Integration.UI.Models;

public class MainWindowModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private bool _isRunning;
    public bool IsRunning {
        get => _isRunning;
        set {
            _isRunning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsReady));
        }
    }

    public bool IsReady => !_isRunning;

    private string _outputMessage;
    public string OutputMessage {
        get => _outputMessage;
        set {
            _outputMessage = value;
            OnPropertyChanged();
        }
    }

    public int MaxThreadCount {get; set;}
    public int WavelengthCount {get; set;}
    public bool SquareScale {get; set;}
    public int TextureSize {get; set;}
    public float Quality {get; set;}
    public float Radius {get; set;}
    public float Scale {get; set;}
    public float Distance {get; set;}

    private ImageSource _previewImage;
    public ImageSource PreviewImage {
        get => _previewImage;
        set {
            _previewImage = value;
            OnPropertyChanged();
        }
    }


    public MainWindowModel()
    {
        MaxThreadCount = Math.Max(Environment.ProcessorCount - 2, 1);
        WavelengthCount = 30;
        SquareScale = false;
        TextureSize = 256;
        Quality = 1.0f;
        Radius = 2.0f;
        Scale = 10.0f;
        Distance = 10.0f;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}