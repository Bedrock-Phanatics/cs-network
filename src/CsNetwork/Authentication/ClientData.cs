using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CsNetwork.Authentication;

public sealed record ClientData(
    DeviceOS DeviceOS,
    string DeviceModel,
    string DeviceId,
    string GameVersion,
    int GuiScale,
    int UIProfile,
    long ClientRandomId,
    string ServerAddress,
    string LanguageCode,
    SkinData Skin,
    int CurrentInputMode = 1,
    int DefaultInputMode = 1,
    string? SelfSignedId = null,
    string? PlatformOfflineId = null,
    string? PlatformOnlineId = null)
{
    public bool Validate([NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrEmpty(GameVersion))
        {
            error = "GameVersion must not be empty.";
            return false;
        }

        if (string.IsNullOrEmpty(ServerAddress))
        {
            error = "ServerAddress must not be empty.";
            return false;
        }

        if ((int)DeviceOS < 1 || (int)DeviceOS > 15)
        {
            error = $"DeviceOS value {(int)DeviceOS} is out of valid range [1, 15].";
            return false;
        }

        if (UIProfile is < 0 or > 2)
        {
            error = $"UIProfile value {UIProfile} is out of valid range [0, 2].";
            return false;
        }

        if (string.IsNullOrEmpty(LanguageCode) || LanguageCode.Length > 32)
        {
            error = $"LanguageCode '{LanguageCode}' is invalid.";
            return false;
        }

        if (Skin == null)
        {
            error = "Skin data must not be null.";
            return false;
        }

        return Skin.Validate(out error);
    }

    public static bool TryParse(string json, [NotNullWhen(true)] out ClientData? clientData, [NotNullWhen(false)] out string? error)
    {
        clientData = null;
        error = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            int deviceOSInt = root.TryGetProperty(nameof(DeviceOS), out var osProp) ? osProp.GetInt32() : (int)DeviceOS.Win10;
            string deviceModel = root.TryGetProperty(nameof(DeviceModel), out var dmProp) ? dmProp.GetString() ?? "" : "";
            string deviceId = root.TryGetProperty(nameof(DeviceId), out var didProp) ? didProp.GetString() ?? "" : "";
            string gameVersion = root.TryGetProperty(nameof(GameVersion), out var gvProp) ? gvProp.GetString() ?? "" : "";
            int guiScale = root.TryGetProperty(nameof(GuiScale), out var gsProp) ? gsProp.GetInt32() : 0;
            int uiProfile = root.TryGetProperty(nameof(UIProfile), out var uipProp) ? uipProp.GetInt32() : 0;
            long clientRandomId = root.TryGetProperty(nameof(ClientRandomId), out var criProp) ? criProp.GetInt64() : 0;
            string serverAddress = root.TryGetProperty(nameof(ServerAddress), out var saProp) ? saProp.GetString() ?? "" : "";
            string languageCode = root.TryGetProperty(nameof(LanguageCode), out var lcProp) ? lcProp.GetString() ?? "en_US" : "en_US";
            int currentInput = root.TryGetProperty(nameof(CurrentInputMode), out var cimProp) ? cimProp.GetInt32() : 1;
            int defaultInput = root.TryGetProperty(nameof(DefaultInputMode), out var dimProp) ? dimProp.GetInt32() : 1;

            string? selfSignedId = root.TryGetProperty(nameof(SelfSignedId), out var ssProp) ? ssProp.GetString() : null;
            string? platformOfflineId = root.TryGetProperty(nameof(PlatformOfflineId), out var pfoProp) ? pfoProp.GetString() : null;
            string? platformOnlineId = root.TryGetProperty(nameof(PlatformOnlineId), out var pfnProp) ? pfnProp.GetString() : null;

            string skinId = root.TryGetProperty("SkinId", out var sIdProp) ? sIdProp.GetString() ?? "" : "";
            string skinDataB64 = root.TryGetProperty("SkinData", out var sdProp) ? sdProp.GetString() ?? "" : "";
            int skinWidth = root.TryGetProperty("SkinImageWidth", out var swProp) ? swProp.GetInt32() : 0;
            int skinHeight = root.TryGetProperty("SkinImageHeight", out var shProp) ? shProp.GetInt32() : 0;

            byte[] skinImage = string.IsNullOrEmpty(skinDataB64) ? [] : Convert.FromBase64String(skinDataB64);

            string? capeDataB64 = root.TryGetProperty("CapeData", out var cdProp) ? cdProp.GetString() : null;
            int capeWidth = root.TryGetProperty("CapeImageWidth", out var cwProp) ? cwProp.GetInt32() : 0;
            int capeHeight = root.TryGetProperty("CapeImageHeight", out var chProp) ? chProp.GetInt32() : 0;
            byte[]? capeImage = string.IsNullOrEmpty(capeDataB64) ? null : Convert.FromBase64String(capeDataB64);

            string? capeId = root.TryGetProperty("CapeId", out var cIdProp) ? cIdProp.GetString() : null;
            string? skinResourcePatch = root.TryGetProperty("SkinResourcePatch", out var srpProp) ? srpProp.GetString() : null;
            string? geometryData = root.TryGetProperty("SkinGeometryData", out var sgProp) ? sgProp.GetString() : null;
            string? geometryVersion = root.TryGetProperty("SkinGeometryDataEngineVersion", out var sgvProp) ? sgvProp.GetString() : null;
            string? animationData = root.TryGetProperty("SkinAnimationData", out var sadProp) ? sadProp.GetString() : null;
            string? skinColor = root.TryGetProperty("SkinColor", out var scProp) ? scProp.GetString() : null;
            string? armSize = root.TryGetProperty("ArmSize", out var asProp) ? asProp.GetString() : "wide";
            string? playFabId = root.TryGetProperty("PlayFabId", out var pfProp) ? pfProp.GetString() : null;

            bool isPremium = root.TryGetProperty("PremiumSkin", out var psProp) && psProp.GetBoolean();
            bool isPersona = root.TryGetProperty("PersonaSkin", out var perProp) && perProp.GetBoolean();
            bool isCapeOnClassic = root.TryGetProperty("CapeOnClassicSkin", out var cocProp) && cocProp.GetBoolean();
            bool isTrusted = !root.TryGetProperty("TrustedSkin", out var tsProp) || tsProp.GetBoolean();

            var skin = new SkinData(
                skinId,
                skinImage,
                skinWidth,
                skinHeight,
                capeImage,
                capeWidth,
                capeHeight,
                capeId,
                skinResourcePatch,
                geometryData,
                geometryVersion,
                animationData,
                skinColor,
                armSize,
                playFabId,
                isPremium,
                isPersona,
                isCapeOnClassic,
                isTrusted);

            if (!skin.Validate(out error))
                return false;

            clientData = new ClientData(
                (DeviceOS)deviceOSInt,
                deviceModel,
                deviceId,
                gameVersion,
                guiScale,
                uiProfile,
                clientRandomId,
                serverAddress,
                languageCode,
                skin,
                currentInput,
                defaultInput,
                selfSignedId,
                platformOfflineId,
                platformOnlineId);

            return clientData.Validate(out error);
        }
        catch (Exception ex)
        {
            error = $"Failed to parse client data JSON: {ex.Message}";
            return false;
        }
    }

    public string ToJson()
    {
        var model = new ClientDataJsonModel
        {
            DeviceOS = (int)DeviceOS,
            DeviceModel = DeviceModel,
            DeviceId = DeviceId,
            GameVersion = GameVersion,
            GuiScale = GuiScale,
            UIProfile = UIProfile,
            ClientRandomId = ClientRandomId,
            ServerAddress = ServerAddress,
            LanguageCode = LanguageCode,
            CurrentInputMode = CurrentInputMode,
            DefaultInputMode = DefaultInputMode,
            SelfSignedId = SelfSignedId ?? "",
            PlatformOfflineId = PlatformOfflineId ?? "",
            PlatformOnlineId = PlatformOnlineId ?? "",
            SkinId = Skin.SkinId,
            SkinData = Convert.ToBase64String(Skin.SkinImage),
            SkinImageWidth = Skin.SkinImageWidth,
            SkinImageHeight = Skin.SkinImageHeight,
            CapeData = Skin.CapeImage != null && Skin.CapeImage.Length > 0 ? Convert.ToBase64String(Skin.CapeImage) : "",
            CapeImageWidth = Skin.CapeImageWidth,
            CapeImageHeight = Skin.CapeImageHeight,
            CapeId = Skin.CapeId ?? "",
            SkinResourcePatch = Skin.SkinResourcePatch ?? "{\"geometry\":{\"default\":\"geometry.humanoid.custom\"}}",
            SkinGeometryData = Skin.GeometryData ?? "",
            SkinGeometryDataEngineVersion = Skin.GeometryDataEngineVersion ?? "",
            SkinAnimationData = Skin.AnimationData ?? "",
            SkinColor = Skin.SkinColor ?? "#0",
            ArmSize = Skin.ArmSize ?? "wide",
            PlayFabId = Skin.PlayFabId ?? "",
            PremiumSkin = Skin.IsPremium,
            PersonaSkin = Skin.IsPersona,
            CapeOnClassicSkin = Skin.IsCapeOnClassicSkin,
            TrustedSkin = Skin.IsTrusted
        };

        return JsonSerializer.Serialize(model);
    }

    private sealed class ClientDataJsonModel
    {
        public int DeviceOS { get; set; }
        public string DeviceModel { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public string GameVersion { get; set; } = "";
        public int GuiScale { get; set; }
        public int UIProfile { get; set; }
        public long ClientRandomId { get; set; }
        public string ServerAddress { get; set; } = "";
        public string LanguageCode { get; set; } = "en_US";
        public int CurrentInputMode { get; set; }
        public int DefaultInputMode { get; set; }
        public string SelfSignedId { get; set; } = "";
        public string PlatformOfflineId { get; set; } = "";
        public string PlatformOnlineId { get; set; } = "";
        public string SkinId { get; set; } = "";
        public string SkinData { get; set; } = "";
        public int SkinImageWidth { get; set; }
        public int SkinImageHeight { get; set; }
        public string CapeData { get; set; } = "";
        public int CapeImageWidth { get; set; }
        public int CapeImageHeight { get; set; }
        public string CapeId { get; set; } = "";
        public string SkinResourcePatch { get; set; } = "";
        public string SkinGeometryData { get; set; } = "";
        public string SkinGeometryDataEngineVersion { get; set; } = "";
        public string SkinAnimationData { get; set; } = "";
        public string SkinColor { get; set; } = "";
        public string ArmSize { get; set; } = "wide";
        public string PlayFabId { get; set; } = "";
        public bool PremiumSkin { get; set; }
        public bool PersonaSkin { get; set; }
        public bool CapeOnClassicSkin { get; set; }
        public bool TrustedSkin { get; set; }
    }
}
