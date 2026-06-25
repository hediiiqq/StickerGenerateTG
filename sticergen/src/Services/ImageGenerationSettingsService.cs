using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using sticergen.Configuration;
using sticergen.Data;
using sticergen.Data.Models;

namespace sticergen.Services;

public class ImageGenerationSettingsService
{
    private const int ActiveSettingId = 1;

    private readonly AppDbContext _db;
    private readonly ImageGenerationOptions _options;

    public ImageGenerationSettingsService(AppDbContext db, IOptions<ImageGenerationOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<ImageGenerationSetting> GetActiveAsync(CancellationToken cancellationToken)
    {
        var setting = await _db.ImageGenerationSettings
            .FirstOrDefaultAsync(x => x.Id == ActiveSettingId, cancellationToken);

        if (setting is not null)
        {
            if (!ImageGenerationModelCatalog.IsSupported(setting.Provider, setting.Model))
            {
                ApplyDefaultSetting(setting);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return setting;
        }

        setting = new ImageGenerationSetting { Id = ActiveSettingId };
        ApplyDefaultSetting(setting);

        _db.ImageGenerationSettings.Add(setting);
        await _db.SaveChangesAsync(cancellationToken);

        return setting;
    }

    public async Task<ImageGenerationSetting> SetActiveAsync(
        string provider,
        string model,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var normalizedModel = model.Trim();

        if (!ImageGenerationModelCatalog.IsSupported(normalizedProvider, normalizedModel))
        {
            throw new InvalidOperationException(
                "Unsupported AI model. Available models:\n" + ImageGenerationModelCatalog.FormatSupportedModels());
        }

        var setting = await GetActiveAsync(cancellationToken);
        setting.Provider = normalizedProvider;
        setting.Model = normalizedModel;
        setting.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return setting;
    }

    private void ApplyDefaultSetting(ImageGenerationSetting setting)
    {
        var provider = _options.Provider.Trim().ToLowerInvariant();
        var model = _options.Model.Trim();

        if (!ImageGenerationModelCatalog.IsSupported(provider, model))
        {
            throw new InvalidOperationException(
                "Default AI model from configuration is unsupported. Available models:\n" +
                ImageGenerationModelCatalog.FormatSupportedModels());
        }

        setting.Provider = provider;
        setting.Model = model;
        setting.UpdatedAt = DateTime.UtcNow;
    }
}
