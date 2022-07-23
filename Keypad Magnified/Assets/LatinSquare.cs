using System.Collections.Generic;
using System.Linq;

//Code originally taken from Timwi and Quinn Wuest's Bunch of Buttons pack
static class LatinSquare
{
    public static int[] Generate(MonoRandom rnd, int width, int height, int max)
    {
        return generateColorGrid(rnd, width, height, new int?[width * height],
            Enumerable.Range(0, width * height).Select(_ => Enumerable.Range(0, max).ToList()).ToArray());
    }

    private static int[] generateColorGrid(MonoRandom rnd, int width, int height, int?[] sofar, List<int>[] available)
    {
        var ixs = new List<int>();
        var lowest = int.MaxValue;
        for (var sq = 0; sq < width * height; sq++)
        {
            if (sofar[sq] != null)
                continue;
            if (available[sq].Count < lowest)
            {
                ixs.Clear();
                ixs.Add(sq);
                lowest = available[sq].Count;
            }
            else if (available[sq].Count == lowest)
                ixs.Add(sq);
            if (lowest == 1)
                break;
        }

        if (ixs.Count == 0)
            return sofar.Select(i => i.Value).ToArray();

        var square = ixs[rnd.Next(0, ixs.Count)];
        var offset = rnd.Next(0, available[square].Count);
        for (var fAvIx = 0; fAvIx < available[square].Count; fAvIx++)
        {
            var avIx = (fAvIx + offset) % available[square].Count;
            var v = available[square][avIx];
            sofar[square] = v;

            var result = generateColorGrid(rnd, width, height, sofar, processAvailable(available, square, v, width, height));
            if (result != null)
                return result;
        }
        sofar[square] = null;
        return null;
    }

    private static List<int>[] processAvailable(List<int>[] available, int sq, int v, int w, int h)
    {
        var newAvailable = available.ToArray();
        for (var c = 0; c < w; c++)
        {
            var avIx = c + w * (sq / w);
            var ix = newAvailable[avIx].IndexOf(v);
            if (ix != -1)
            {
                newAvailable[avIx] = newAvailable[avIx].ToList();
                newAvailable[avIx].RemoveAt(ix);
            }
        }
        for (var r = 0; r < h; r++)
        {
            var avIx = (sq % w) + w * r;
            var ix = newAvailable[avIx].IndexOf(v);
            if (ix != -1)
            {
                newAvailable[avIx] = newAvailable[avIx].ToList();
                newAvailable[avIx].RemoveAt(ix);
            }
        }
        return newAvailable;
    }
}
