using Diffraction_Monte_Carlo_Integration.UI.Internal;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Diffraction_Monte_Carlo_Integration.UI.Models;

public class MainWindowModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public static readonly int MaxThreadCountDefault;

    private bool _isRunning;
    private string _outputMessage;
    private int _buildProgress;
    private int? _maxThreadCount;
    private int? _wavelengthCount;
    private int? _textureSize;
    private Image<Rgb24> _previewImage;
    private float? _quality;
    private float? _radius;
    private float? _scale;
    private float? _distance;
    private int? _bladeCount;
    private bool _squareScale;
    private ImageSource _previewImageSource;
    private double _zoom;

    public bool IsRunning {
        get => _isRunning;
        set {
            _isRunning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsReady));
        }
    }

    public string OutputMessage {
        get => _outputMessage;
        set {
            _outputMessage = value;
            OnPropertyChanged();
        }
    }

    public int BuildProgress {
        get => _buildProgress;
        set {
            _buildProgress = value;
            OnPropertyChanged();
        }
    }

    public int? MaxThreadCount {
        get => _maxThreadCount;
        set {
            if (value.HasValue) {
                if (value.Value < 1) throw new ApplicationException("Max Thread Count must be greater than zero!");
                if (value.Value > 99) throw new ApplicationException("Max Thread Count must be less than 100!");
            }

            _maxThreadCount = value;
            OnPropertyChanged();
        }
    }

    public int? WavelengthCount {
        get => _wavelengthCount;
        set {
            if (value.HasValue) {
                if (value.Value < 1) throw new ApplicationException("Wavelength Count must be greater than zero!");
                if (value.Value > 441) throw new ApplicationException("Wavelength Count must be less than or equal to 441!");
            }

            _wavelengthCount = value;
            OnPropertyChanged();
        }
    }

    public int? TextureSize {
        get => _textureSize;
        set {
            _textureSize = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ActualTextureSize));
        }
    }

    public Image<Rgb24> PreviewImage {
        get => _previewImage;
        set {
            _previewImage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ActualTextureSize));
        }
    }

    public float? Quality {
        get => _quality;
        set {
            if (value.HasValue) {
                if (value.Value < float.Epsilon) throw new ApplicationException("Quality must be greater than zero!");
                if (value.Value > 100.0 - float.Epsilon) throw new ApplicationException("Quality must be less than 100!");
            }

            _quality = value;
            OnPropertyChanged();
        }
    }

    public float? Radius {
        get => _radius;
        set {
            if (value.HasValue) {
                if (value.Value < float.Epsilon) throw new ApplicationException("Radius must be greater than zero!");
                if (value.Value > 100.0 - float.Epsilon) throw new ApplicationException("Radius must be less than 100!");
            }

            _radius = value;
            OnPropertyChanged();
        }
    }

    public float? Scale {
        get => _scale;
        set {
            if (value.HasValue) {
                if (value.Value < float.Epsilon) throw new ApplicationException("Scale must be greater than zero!");
                if (value.Value > 100.0 - float.Epsilon) throw new ApplicationException("Scale must be less than 100!");
            }

            _scale = value;
            OnPropertyChanged();
        }
    }

    public float? Distance {
        get => _distance;
        set {
            if (value.HasValue) {
                if (value.Value < float.Epsilon) throw new ApplicationException("Distance must be greater than zero!");
                if (value.Value > 100.0 - float.Epsilon) throw new ApplicationException("Distance must be less than 100!");
            }

            _distance = value;
            OnPropertyChanged();
        }
    }

    public int? BladeCount {
        get => _bladeCount;
        set {
            if (value.HasValue) {
                if (value.Value < 1) throw new ApplicationException("Blade Count must be greater than zero!");
                if (value.Value > 99) throw new ApplicationException("Blade Count must be less than 100!");
            }

            _bladeCount = value;
            OnPropertyChanged();
        }
    }

    public bool SquareScale {
        get => _squareScale;
        set {
            _squareScale = value;
            OnPropertyChanged();
        }
    }

    public ImageSource PreviewImageSource {
        get => _previewImageSource;
        set {
            _previewImageSource = value;
            OnPropertyChanged();
        }
    }

    public double Zoom {
        get => _zoom;
        set {
            _zoom = value;
            OnPropertyChanged();

            OnPropertyChanged(nameof(ZoomText));
        }
    }

    public string ZoomText {
        get => ZoomHelper.Format(_zoom);
        set {
            _zoom = ZoomHelper.Parse(value);
            OnPropertyChanged();

            OnPropertyChanged(nameof(Zoom));
        }
    }

    public bool IsReady => !_isRunning;
    public int ActualTextureSize => PreviewImage?.Width ?? _textureSize ?? 256;
    public string MaxThreadCountLabel => $"Max Thread Count (default: {MaxThreadCountDefault})";


    static MainWindowModel()
    {
        MaxThreadCountDefault = Math.Max(Environment.ProcessorCount - 2, 1);
    }

    public MainWindowModel()
    {
        SquareScale = false;
        _zoom = 1d;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
