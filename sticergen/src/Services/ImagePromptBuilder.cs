namespace sticergen.Services;

public static class ImagePromptBuilder
{
    public const string StickerNegativePrompt =
        "photorealistic, realistic photo, raw photo, selfie, camera photo, dslr, lens blur, skin pores, " +
        "realistic skin texture, photographic lighting, old ai generated photo, uncanny face, low stylization, " +
        "different person, changed identity, different face, changed face shape, changed hairstyle, changed pose, " +
        "changed clothes, added costume, bad face, distorted face, deformed eyes, extra eyes, bad anatomy, " +
        "mask, glasses, goggles, helmet, extra accessories, weapon, text, letters, words, caption, " +
        "logo, watermark, user interface, screenshot, photo frame, blurry, low quality, cropped head, duplicate person";

    public static string BuildImageToImageStickerPrompt(string stylePrompt, string model)
    {
        if (model.StartsWith("@cf/", StringComparison.OrdinalIgnoreCase))
        {
            return BuildCloudflareImageToImageStickerPrompt(stylePrompt);
        }

        return
            "Transform the input portrait into a finished Telegram sticker. " +
            "This must be an illustrated sticker, not a realistic photo and not a lightly edited selfie. " +
            $"User style request: \"{stylePrompt}\". Interpret this short phrase as the main visual style. " +
            $"{BuildStyleDirection(stylePrompt)} " +
            "Preserve the person's identity, face shape, expression, pose, headwear, clothing silhouette, and main colors, " +
            "but redraw everything as clean stylized character art. " +
            "Use bold readable shapes, simplified facial details, confident linework, clean cel shading, high contrast, " +
            "centered upper-body composition, simple background, and a clear white sticker border. " +
            $"Model tuning target: {model}. " +
            "Do not keep photographic skin texture, camera noise, realistic background detail, captions, letters, logos, UI, screenshots, or watermarks.";
    }

    private static string BuildCloudflareImageToImageStickerPrompt(string stylePrompt)
    {
        var normalizedPrompt = stylePrompt.Trim();

        return
            "Use the input image as a strict reference. Preserve the same person, face shape, hairstyle, expression, " +
            "pose, camera angle, clothing silhouette, clothing colors, and overall composition. Make only a controlled " +
            "style conversion, not a new character and not a new scene. Convert the original photo into a clean modern " +
            "Telegram sticker illustration with crisp white sticker outline, clean line art, flat colors, and light cel shading. " +
            $"Apply this visual style without changing identity: {normalizedPrompt}. " +
            "Keep the result close to the original image. Do not invent masks, helmets, glasses, costumes, props, symbols, " +
            "background objects, text, logos, or extra accessories. Not photorealistic, not 3D render.";
    }

    public static string BuildTextToImageStickerPrompt(string stylePrompt, string model)
    {
        return
            "Create a finished Telegram sticker. " +
            $"User style request: \"{stylePrompt}\". Interpret this short phrase as the main visual style. " +
            $"{BuildStyleDirection(stylePrompt)} " +
            "Create one centered expressive character or object that best matches the request. " +
            "Use clean stylized character art, bold readable shapes, simplified details, high contrast, simple background, " +
            "and a clear white sticker border. " +
            $"Model tuning target: {model}. " +
            "Do not add captions, letters, logos, UI, screenshots, watermarks, or realistic photo texture.";
    }

    private static string BuildStyleDirection(string stylePrompt)
    {
        var normalizedPrompt = stylePrompt.Trim();

        return
            $"If \"{normalizedPrompt}\" is vague, choose a coherent visual interpretation with matching clothes, palette, lighting, and mood. " +
            "Only add style-related details that clearly fit the request; avoid random masks, helmets, glasses, symbols, and unrelated props.";
    }
}
