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
    public event EventHandler ExposureChanged;

    public static readonly int MaxThreadCountDefault;

    private bool _isRunning;
    private string _outputMessage;
    private int _buildProgress;
    private int? _maxThreadCount;
    private int? _streamCount;
    private int? _wavelengthCount;
    private int? _textureSize;
    private Image<Rgb24> _previewImage;
    private float _quality;
    private float? _radius;
    private float? _scale;
    private float? _distance;
    private int? _bladeCount;
    private bool _squareScale;
    private ImageSource _previewImageSource;
    private double _zoom;
    private double? _exposure;

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
                if (value.Value > Environment.ProcessorCount*2) throw new ApplicationException($"Max Thread Count must be less than {Environment.ProcessorCount*2:N0}!");
            }

            _maxThreadCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StreamCountLabel));
        }
    }

    public int? StreamCount {
        get => _streamCount;
        set {
            if (value.HasValue) {
                if (value.Value < 1) throw new ApplicationException("Stream Count must be greater than zero!");
                if (value.Value > 16) throw new ApplicationException("Stream Count must be less than or equal to 16!");
            }

            _streamCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StreamCountLabel));
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

    public float Quality {
        get => _quality;
        set {
            if (value < 0.1f) throw new ApplicationException("Quality must be greater than or equal to 0.1!");
            if (value > 10f) throw new ApplicationException("Quality must be less than or equal to 10!");

            _quality = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(QualityLabel));
        }
    }

    public float? Radius {
        get => _radius;
        set {
            if (value.HasValue) {
                if (value.Value < 1f) throw new ApplicationException("Radius must be greater than or equal to 1!");
                if (value.Value > 100f) throw new ApplicationException("Radius must be less than or equal to 100!");
            }

            _radius = value;
            OnPropertyChanged();
        }
    }

    public float? Scale {
        get => _scale;
        set {
            if (value.HasValue) {
                if (value.Value < 1f) throw new ApplicationException("Scale must be greater than or equal to 1!");
                if (value.Value > 5_000f) throw new ApplicationException("Scale must be less than or equal to 5,000!");
            }

            _scale = value;
            OnPropertyChanged();
        }
    }

    public float? Distance {
        get => _distance;
        set {
            if (value.HasValue) {
                if (value.Value < 1f) throw new ApplicationException("Distance must be greater than or equal to 1!");
                if (value.Value > 10_000f) throw new ApplicationException("Distance must be less than or equal to 10,000!");
            }

            _distance = value;
            OnPropertyChanged();
        }
    }

    public int? BladeCount {
        get => _bladeCount;
        set {
            if (value.HasValue) {
                if (value.Value < 3) throw new ApplicationException("Blade Count must be greater than or equal to 3!");
                if (value.Value > 32) throw new ApplicationException("Blade Count must be less than or equal to 32!");
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

    public double? Exposure {
        get => _exposure;
        set {
            _exposure = value;
            OnPropertyChanged();

            OnExposureChanged();
        }
    }

    public bool IsReady => !_isRunning;
    public int ActualTextureSize => PreviewImage?.Width ?? _textureSize ?? 256;
    public string QualityLabel => $"Quality: {Quality:N1}";
    public string MaxThreadCountLabel => $"Max Thread Count (default: {MaxThreadCountDefault})";
    public string StreamCountLabel => $"Stream Count (default: {Math.Min(_maxThreadCount ?? MaxThreadCountDefault, 16)})";


    static MainWindowModel()
    {
        MaxThreadCountDefault = Math.Max(Environment.ProcessorCount - 2, 1);
    }

    public MainWindowModel()
    {
        SquareScale = false;
        _quality = 1f;
        _zoom = 1d;
    }

    protected virtual void OnExposureChanged()
    {
        ExposureChanged?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
