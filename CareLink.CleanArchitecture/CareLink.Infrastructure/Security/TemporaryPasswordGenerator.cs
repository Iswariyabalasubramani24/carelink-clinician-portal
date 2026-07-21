using System.Security.Cryptography;
using CareLink.Application.Common.Interfaces;

namespace CareLink.Infrastructure.Security;

public class TemporaryPasswordGenerator : ITemporaryPasswordGenerator
{
    private const int Length = 12;

    // Visually-ambiguous characters (0/O, 1/l/I) are excluded since an admin
    // typically reads this aloud or copy-pastes it to share with the new user.
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Special = "!@#$%^&*";
    private const string All = Upper + Lower + Digits + Special;

    public string Generate()
    {
        var chars = new char[Length];

        // Guarantee at least one character from each required category.
        chars[0] = Upper[RandomNumberGenerator.GetInt32(Upper.Length)];
        chars[1] = Lower[RandomNumberGenerator.GetInt32(Lower.Length)];
        chars[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        chars[3] = Special[RandomNumberGenerator.GetInt32(Special.Length)];

        for (var i = 4; i < Length; i++)
        {
            chars[i] = All[RandomNumberGenerator.GetInt32(All.Length)];
        }

        // Fisher-Yates shuffle so the guaranteed characters aren't always in
        // the first four positions.
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
