using FlowControlModel;
using FlowControlModel.Factories;
using FlowControlModel.World;

namespace FlowControlBusiness.WorldGeneration;

/// <summary>
/// Deterministic value-noise ground generator: lakes and smooth stone patches on grass,
/// with rare iron deposit veins — each from its own independent noise channel.
/// The same seed and coordinate always produce the same ground kind.
/// Requires the <c>"water"</c>, <c>"stone"</c>, <c>"grass"</c> and <c>"iron_deposit"</c>
/// grounds to be registered.
/// </summary>
public class NoiseWorldGenerator(Registry registry, int seed = 0) : IWorldGenerator
{
    /// <summary> Noise frequency: smaller values produce larger patches. </summary>
    private const float Frequency = 0.09f;

    /// <summary> Noise values above this threshold become stone. </summary>
    private const float StoneThreshold = 0.62f;

    /// <summary> Frequency of the deposit noise channel: smaller veins than the stone patches. </summary>
    private const float DepositFrequency = 0.17f;

    /// <summary> Deposit noise values above this threshold become iron deposits. </summary>
    private const float DepositThreshold = 0.85f;

    /// <summary> Seed salt separating the deposit channel from the stone channel. </summary>
    private const int DepositChannel = 0x5EED;

    /// <summary> Frequency of the water channel: low, so lakes come out large. </summary>
    private const float WaterFrequency = 0.06f;

    /// <summary> Water noise values below this threshold become lakes. </summary>
    private const float WaterThreshold = 0.17f;

    /// <summary> Seed salt separating the water channel from the other channels. </summary>
    private const int WaterChannel = 0x7A7E2;

    private readonly GroundLite _water = registry.GetGroundLite("water");
    private readonly GroundLite _stone = registry.GetGroundLite("stone");
    private readonly GroundLite _grass = registry.GetGroundLite("grass");
    private readonly GroundLite _ironDeposit = registry.GetGroundLite("iron_deposit");

    /// <summary> Returns the ground kind at the specified global cell. </summary>
    public GroundLite GetGroundAt(Vec2I coord)
    {
        if (ValueNoise(coord.X * WaterFrequency, coord.Y * WaterFrequency, seed ^ WaterChannel)
            < WaterThreshold)
            return _water;

        if (ValueNoise(coord.X * DepositFrequency, coord.Y * DepositFrequency, seed ^ DepositChannel)
            > DepositThreshold)
            return _ironDeposit;

        return ValueNoise(coord.X * Frequency, coord.Y * Frequency, seed) > StoneThreshold
            ? _stone
            : _grass;
    }

    /// <summary> Smoothly interpolated lattice noise in [0, 1). </summary>
    private static float ValueNoise(float x, float y, int channelSeed)
    {
        int x0 = MathM.FloorToInt(x), y0 = MathM.FloorToInt(y);
        float tx = MathM.SmoothStep(x - x0);
        float ty = MathM.SmoothStep(y - y0);

        float v00 = Hash(x0, y0, channelSeed), v10 = Hash(x0 + 1, y0, channelSeed);
        float v01 = Hash(x0, y0 + 1, channelSeed), v11 = Hash(x0 + 1, y0 + 1, channelSeed);
        return MathM.Lerp(MathM.Lerp(v00, v10, tx), MathM.Lerp(v01, v11, tx), ty);
    }

    /// <summary> Deterministic pseudo-random value in [0, 1) for a lattice point. </summary>
    private static float Hash(int x, int y, int channelSeed)
    {
        unchecked
        {
            var h = (uint)channelSeed;
            h ^= (uint)x * 0x9E3779B9u;
            h = (h ^ (h >> 16)) * 0x85EBCA6Bu;
            h ^= (uint)y * 0xC2B2AE35u;
            h = (h ^ (h >> 13)) * 0x27D4EB2Fu;
            h ^= h >> 16;
            return h / 4294967296f;
        }
    }
}
