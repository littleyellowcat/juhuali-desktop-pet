using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace JuHuaLiPet;

internal sealed class SpriteAnimator
{
    private const int CellWidth = 192;
    private const int CellHeight = 208;

    private readonly Image _target;
    private readonly BitmapImage _spritesheet;
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<PetAnimationState, AnimationFrame[]> _animations;
    private PetAnimationState _state = PetAnimationState.Idle;
    private int _frameIndex;
    private DispatcherTimer? _temporaryTimer;

    public SpriteAnimator(Image target)
    {
        _target = target;
        _spritesheet = new BitmapImage(new Uri("pack://application:,,,/Assets/juhuali-spritesheet.png"));
        _animations = BuildAnimations();
        _timer = new DispatcherTimer();
        _timer.Tick += (_, _) => AdvanceFrame();
        SetState(PetAnimationState.Idle);
        _timer.Start();
    }

    public void SetState(PetAnimationState state)
    {
        if (_state == state && _target.Source != null)
        {
            return;
        }

        _temporaryTimer?.Stop();
        _temporaryTimer = null;
        _state = state;
        _frameIndex = 0;
        ShowCurrentFrame();
    }

    public void PlayTemporary(PetAnimationState state, TimeSpan duration)
    {
        SetState(state);
        _temporaryTimer = new DispatcherTimer { Interval = duration };
        _temporaryTimer.Tick += (_, _) =>
        {
            _temporaryTimer?.Stop();
            _temporaryTimer = null;
            SetState(PetAnimationState.Idle);
        };
        _temporaryTimer.Start();
    }

    private void AdvanceFrame()
    {
        var frames = _animations[_state];
        _frameIndex = (_frameIndex + 1) % frames.Length;
        ShowCurrentFrame();
    }

    private void ShowCurrentFrame()
    {
        var frame = _animations[_state][_frameIndex];
        var rect = new Int32Rect(frame.Column * CellWidth, frame.Row * CellHeight, CellWidth, CellHeight);
        _target.Source = new CroppedBitmap(_spritesheet, rect);
        _timer.Interval = TimeSpan.FromMilliseconds(frame.DurationMs);
    }

    private static Dictionary<PetAnimationState, AnimationFrame[]> BuildAnimations()
    {
        return new Dictionary<PetAnimationState, AnimationFrame[]>
        {
            [PetAnimationState.Idle] =
            [
                new(0, 0, 420),
                new(0, 1, 130),
                new(0, 2, 140),
                new(0, 3, 160),
                new(0, 4, 160),
                new(0, 5, 460),
            ],
            [PetAnimationState.RunningRight] = Frames(1, 8, 115, 180),
            [PetAnimationState.RunningLeft] = Frames(2, 8, 115, 180),
            [PetAnimationState.Waving] = Frames(3, 4, 150, 260),
            [PetAnimationState.Jumping] = Frames(4, 5, 135, 240),
            [PetAnimationState.Failed] = Frames(5, 8, 150, 260),
            [PetAnimationState.Waiting] = Frames(6, 6, 150, 260),
            [PetAnimationState.Running] = Frames(7, 6, 115, 220),
            [PetAnimationState.Review] = Frames(8, 6, 150, 260),
        };
    }

    private static AnimationFrame[] Frames(int row, int count, int durationMs, int lastDurationMs)
    {
        return Enumerable.Range(0, count)
            .Select(index => new AnimationFrame(row, index, index == count - 1 ? lastDurationMs : durationMs))
            .ToArray();
    }

    private readonly record struct AnimationFrame(int Row, int Column, int DurationMs);
}
