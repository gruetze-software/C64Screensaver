using System;
using SkiaSharp;
namespace C64Screensaver;
public readonly record struct CrtParameters(bool Enabled, bool FillScreen, bool KeepAspectRatio, float Curvature, float Scanlines,
    float Mask, float Softness, float Glow, float GlowSpread, float Vignette, float Flicker)
{
    public static CrtParameters From(SaverSettings s) => new(s.CrtEnabled, s.FillScreen, s.UsesFourThree,
        (float)s.Curvature / 100, s.Scanlines ? (float)s.ScanlineStrength / 100 : 0,
        (float)s.MaskStrength / 100, (float)s.Softness / 100,
        (float)s.Glow / 100, (float)s.GlowSpread / 100, (float)s.Vignette / 100, (float)s.Flicker / 100);
}

public static class CrtRenderer
{
    private static readonly Lazy<SKRuntimeEffect?> Effect = new(Compile);
    private static readonly Lazy<SKRuntimeEffect?> EmissionEffect = new(() => SKRuntimeEffect.CreateShader(EmissionShader, out _));
    public static string? ShaderError { get; private set; }
    public static bool ShaderAvailable => Effect.Value != null;
    private static SKRuntimeEffect? Compile()
    {
        var effect = SKRuntimeEffect.CreateShader(Shader, out string error);
        ShaderError = effect == null ? error : null;
        return effect;
    }
    public static void Draw(SKCanvas canvas, SKRect bounds, SKImage image, CrtParameters parameters, double time, bool previewDetails = false)
    {
        int saved = canvas.Save();
        try
        {
            float width = image.Width, height = image.Height;
            float targetWidth = (float)bounds.Width, targetHeight = (float)bounds.Height;
            float left = 0, top = 0;
            if (parameters.KeepAspectRatio)
            {
                targetWidth = Math.Min(targetWidth, targetHeight * 4 / 3);
                targetHeight = targetWidth * 3 / 4;
                if (!parameters.FillScreen)
                {
                    // Keep the original breathing space and slow tube movement.
                    float scale = (float)Math.Min(bounds.Width / 408, bounds.Height / 312);
                    targetWidth = 384 * scale;
                    targetHeight = 288 * scale;
                    // Keep the raster registered to the glass; drifting by fractions
                    // of an output pixel creates traveling scanline interference.
                }
                left += ((float)bounds.Width - targetWidth) / 2;
                top += ((float)bounds.Height - targetHeight) / 2;
            }
            float scaleX = targetWidth / width, scaleY = targetHeight / height;
            canvas.Translate(left, top);
            canvas.Scale(scaleX, scaleY);
            if (!parameters.Enabled || Effect.Value is not { } effect)
            {
                canvas.DrawImage(image, 0, 0);
                return;
            }
            using var source = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp,
                new SKSamplingOptions(SKFilterMode.Linear));
            var uniforms = new SKRuntimeEffectUniforms(effect)
            {
                ["previewDetails"] = previewDetails ? 1f : 0f,
                ["curvature"] = parameters.FillScreen ? 0f : parameters.Curvature,
                ["imageWidth"] = (float)image.Width,
                ["imageHeight"] = (float)image.Height,
                ["edgeFade"] = parameters.FillScreen ? 0f : 1f,
                ["pixelScaleY"] = Math.Max(0.1f, canvas.TotalMatrix.ScaleY),
                ["scanlines"] = parameters.Scanlines,
                ["mask"] = parameters.Mask,
                ["softness"] = parameters.Softness,
                ["glow"] = parameters.Glow,
                ["vignette"] = parameters.Vignette,
                ["flicker"] = parameters.Flicker,
                ["time"] = (float)time,
                ["pixelScale"] = Math.Max(0.1f, canvas.TotalMatrix.ScaleX)
            };
            using var bloomImage = parameters.Glow > 0 ? CreateBloom(image, source, parameters) : null;
            uniforms["glow"] = bloomImage == null ? 0f : parameters.Glow;
            using var bloomSource = (bloomImage ?? image).ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp,
                new SKSamplingOptions(SKFilterMode.Linear));
            var children = new SKRuntimeEffectChildren(effect) { ["picture"] = source, ["bloomPicture"] = bloomSource };
            using var shader = effect.ToShader(uniforms, children);
            using var paint = new SKPaint { Shader = shader };
            canvas.DrawRect(0, 0, image.Width, image.Height, paint);
        }
        finally { canvas.RestoreToCount(saved); }
    }

    private static SKImage? CreateBloom(SKImage image, SKShader source, CrtParameters parameters)
    {
        if (EmissionEffect.Value is not { } emission) return null;
        using var surface = SKSurface.Create(new SKImageInfo(image.Width, image.Height));
        if (surface == null) return null;
        var children = new SKRuntimeEffectChildren(emission) { ["picture"] = source };
        using var shader = emission.ToShader(new SKRuntimeEffectUniforms(emission), children);
        using var blur = SKImageFilter.CreateBlur(0.5f + parameters.GlowSpread * 3.5f,
            0.5f + parameters.GlowSpread * 3.5f, SKShaderTileMode.Clamp);
        using var paint = new SKPaint { Shader = shader, ImageFilter = blur };
        surface.Canvas.DrawRect(0, 0, image.Width, image.Height, paint);
        return surface.Snapshot();
    }
    private const string EmissionShader = """
uniform shader picture;
half4 main(float2 p) {
    float3 light = pow(max(picture.eval(p).rgb,float3(0)),float3(2.2));
    float luminance = dot(light,float3(0.2126,0.7152,0.0722));
    return half4(light * smoothstep(0.06,0.38,luminance),1.0);
}
""";
    // Original SKSL implementation. Emulator shaders were studied for their
    // controls and rendering stages; no GPL shader source is incorporated.
    private const string Shader = """
uniform shader picture;
uniform shader bloomPicture;
uniform float previewDetails;
uniform float curvature;
uniform float imageWidth;
uniform float imageHeight;
uniform float edgeFade;
uniform float pixelScaleY;
uniform float scanlines;
uniform float mask;
uniform float softness;
uniform float glow;

uniform float vignette;
uniform float flicker;
uniform float time;
uniform float pixelScale;

half4 main(float2 pos) {
    float2 size = float2(imageWidth,imageHeight);
    float2 q = pos / size * 2.0 - 1.0;
    float2 bent = q * (1.0 + curvature * 0.12 * float2(q.y*q.y,q.x*q.x));
    float2 uv = bent * 0.5 + 0.5;
    if (min(uv.x,uv.y) < 0.0 || max(uv.x,uv.y) > 1.0) return half4(0,0,0,1);
    float edgeAA = clamp(min(min(uv.x,1.0-uv.x)*size.x,min(uv.y,1.0-uv.y)*size.y)*pixelScale,0.0,1.0);
    edgeAA = mix(1.0,edgeAA,edgeFade);
    float2 at = uv * size;
    float3 crisp = picture.eval(float2(floor(at.x) + 0.5,at.y)).rgb;
    float3 smoothColor = picture.eval(at).rgb;
    float3 color = mix(crisp,smoothColor,softness);
    float3 linear = pow(max(color,float3(0)),float3(2.2));
    float3 bloom = glow > 0.0 ? bloomPicture.eval(at).rgb * glow * 1.7 : float3(0);
    // The small settings thumbnail illustrates details at readable spacing.
    // The enlarged preview and screensaver retain source-row scanline spacing.
    float scanRow = at.y;
    if (previewDetails > 0.5 && pixelScaleY < 2.5)
        scanRow = at.y * pixelScaleY / 2.5;
    // Integrate the periodic beam over an output pixel, including curvature's
    // vertical stretch. This suppresses broad moire bands at fractional scaling.
    float footprint = (1.0 + curvature * 0.12 * q.x*q.x) / max(pixelScaleY,0.1);
    if (previewDetails > 0.5 && pixelScaleY < 2.5) footprint = 1.0 / 2.5;
    float phaseWidth = 3.14159265 * min(footprint,1.0);
    float amplitude = sin(phaseWidth) / max(phaseWidth,0.0001);
    float beam = 0.5 + 0.5*cos(6.2831853*(scanRow-0.5))*amplitude;
    float visibility = previewDetails > 0.5 ? 1.0 : smoothstep(1.2,2.1,pixelScaleY);
    linear *= 1.0 - scanlines * 0.55 * (1.0-beam) * visibility;
    float stripe = mod(floor(pos.x*pixelScale),3.0);
    float3 grille = stripe < 1.0 ? float3(1.0,0.65,0.65) :
                    stripe < 2.0 ? float3(0.65,1.0,0.65) : float3(0.65,0.65,1.0);
    linear *= mix(float3(1.0),grille,mask);
    linear += bloom;
    float edge = min(1.0,dot(q,q)*0.5);
    linear *= 1.0 - vignette * 0.35 * edge * edge;
    linear *= 1.0 - flicker * 0.03 * (0.5 + 0.5*sin(time*11.0));
    return half4(pow(max(linear,float3(0)),float3(1.0/2.2))*edgeAA,1.0);
}
""";
}
