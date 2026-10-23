// +---------------------------------------------------------------------------+
// |                                                                           |
// |       MadChess is developed by Erik Madsen.  Copyright 2012 - 2026.       |
// |       MadChess is free software.  It is distributed under the MIT         |
// |       license.  See LICENSE.md file for details.                          |
// |       See https://www.madchess.net/ for user and developer guides.        |
// |                                                                           |
// +---------------------------------------------------------------------------+


using System.Collections.Generic;
#pragma warning disable IDE0047


namespace ErikTheCoder.MadChess.Core.Utilities
{


public static class Extensions
{
    public static bool IsNullOrEmpty(this string text) => string.IsNullOrEmpty(text);


    public static bool IsNullOrEmpty<T>(this List<T> list) => (list == null) || (list.Count == 0);
}
}
