using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace CatGuard;

internal partial class CatMascot : UserControl
{
    private bool requested = true;
    private bool running;
    public CatMascot()
    {
        InitializeComponent();
        IsVisibleChanged += (_, _) => UpdateMotion();
        Loaded += (_, _) => UpdateMotion();
        Unloaded += (_, _) => StopMotion();
    }

    public bool Animate
    {
        get => requested;
        set { requested = value; UpdateMotion(); }
    }

    internal double TailAngle => TailRotate.Angle;
    internal double BreathScale => BodyScale.ScaleY;

    private void UpdateMotion()
    {
        if (requested && IsVisible) StartMotion();
        else StopMotion();
    }

    private void StartMotion()
    {
        if (running) return;
        running = true;
        BodyScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, Loop(1, 1.025, 2.4));
        TailRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, Loop(-8, 9, 1.75));
        var blink = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(5), RepeatBehavior = RepeatBehavior.Forever };
        blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        blink.KeyFrames.Add(new EasingDoubleKeyFrame(0.08, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3.8))));
        blink.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(4.02))));
        EyeScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, blink);
    }

    private static DoubleAnimation Loop(double from, double to, double seconds) => new(from, to, TimeSpan.FromSeconds(seconds))
    { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } };

    private void StopMotion()
    {
        if (!running) return;
        running = false;
        BodyScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, null);
        TailRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null);
        EyeScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, null);
        HeadRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null);
        HeadRotate.Angle = 0;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!requested || !running) return;
        double x = e.GetPosition(this).X / Math.Max(1, ActualWidth);
        HeadRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty,
            new DoubleAnimation(Math.Clamp((x - 0.5) * 12, -6, 6), TimeSpan.FromMilliseconds(180)));
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (running) HeadRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty,
            new DoubleAnimation(0, TimeSpan.FromMilliseconds(260)));
    }
}
