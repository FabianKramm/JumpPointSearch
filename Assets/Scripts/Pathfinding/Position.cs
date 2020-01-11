namespace Pathfinding
{
    public struct Position
    {
        public static readonly Position invalid = new Position(-1, -1);

        public int x;
        public int y;

        public Position(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override int GetHashCode()
        {
            return x * y + y;
        }

        public override bool Equals(object other)
        {
            if (!(other is Position))
                return false;
            Position vector2d = (Position)other;
            if (this.x.Equals(vector2d.x))
                return this.y.Equals(vector2d.y);
            else
                return false;
        }

        public static bool operator ==(Position a, Position b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(Position a, Position b)
        {
            return !(a == b);
        }

        public bool IsValid()
        {
            return x != -1;
        }

        public override string ToString()
        {
            return x + " - " + y;
        }
    }
}