namespace Auth.UseCases.Features;

public record FeatureUseCases(
    CreateFeature CreateFeature,
    GetFeature GetFeature,
    ListFeatures ListFeatures
);