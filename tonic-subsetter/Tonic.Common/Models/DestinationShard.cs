namespace Tonic.Common.Models;

public record DestinationShard<T>(int Remainder, T Discriminant, int Modulus);