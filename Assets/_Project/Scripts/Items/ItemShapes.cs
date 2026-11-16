namespace Project05.Items
{
    /// <summary>
    /// Footprint geometry (design/gdd/grid-backpack.md, Formula 1). Cells use x to the right and y down. A quarter turn
    /// is clockwise on screen: q(x, y) = (-y, x), then the result is shifted so it touches x = 0 and y = 0.
    /// </summary>
    public static class ItemShapes
    {
        /// <summary>Rotates a footprint by whole quarter turns, clockwise on screen.</summary>
        /// <param name="cells">The footprint of the item at rotation 0.</param>
        /// <param name="quarterTurns">Any integer; it is reduced modulo 4.</param>
        /// <returns>A new array of cells whose smallest x and y are both 0.</returns>
        public static CellRecord[] Rotate(CellRecord[] cells, int quarterTurns)
        {
            int turns = ((quarterTurns % 4) + 4) % 4;
            var result = new CellRecord[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                int x = cells[i].x;
                int y = cells[i].y;
                for (int t = 0; t < turns; t++)
                {
                    int nx = -y;
                    int ny = x;
                    x = nx;
                    y = ny;
                }

                result[i] = new CellRecord { x = x, y = y };
            }

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            foreach (CellRecord c in result)
            {
                if (c.x < minX) minX = c.x;
                if (c.y < minY) minY = c.y;
            }

            foreach (CellRecord c in result)
            {
                c.x -= minX;
                c.y -= minY;
            }

            return result;
        }

        /// <summary>Width and height in cells of a footprint's bounding box.</summary>
        public static (int width, int height) Size(CellRecord[] cells)
        {
            int w = 0;
            int h = 0;
            foreach (CellRecord c in cells)
            {
                if (c.x + 1 > w) w = c.x + 1;
                if (c.y + 1 > h) h = c.y + 1;
            }

            return (w, h);
        }
    }
}
