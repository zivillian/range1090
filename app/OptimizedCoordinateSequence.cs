using NetTopologySuite.Geometries;

public class OptimizedCoordinateSequence : CoordinateSequence
{
    private readonly List<Coordinate> _coordinates;

    public OptimizedCoordinateSequence(ICollection<Coordinate> coordinates) : base(coordinates.Count, 2, 0)
    {
        _coordinates = new List<Coordinate>(coordinates);
    }

    public override Coordinate GetCoordinate(int i)
    {
        return _coordinates[i];
    }

    public override double GetX(int index)
    {
        return _coordinates[index].X;
    }

    public override double GetY(int index)
    {
        return _coordinates[index].Y;
    }

    public override CoordinateSequence Reversed()
    {
        _coordinates.Reverse();
        return this;
    }

    #region NotImplemented

    public override double GetOrdinate(int index, int ordinateIndex)
    {
        throw new NotImplementedException();
    }

    public override void SetOrdinate(int index, int ordinateIndex, double value)
    {
        throw new NotImplementedException();
    }

    public override CoordinateSequence? Copy()
    {
        throw new NotImplementedException();
    }

    public override Coordinate CreateCoordinate()
    {
        throw new NotImplementedException();
    }

    public override Envelope ExpandEnvelope(Envelope env)
    {
        throw new NotImplementedException();
    }

    public override void GetCoordinate(int index, Coordinate coord)
    {
        throw new NotImplementedException();
    }

    public override Coordinate GetCoordinateCopy(int i)
    {
        throw new NotImplementedException();
    }

    public override double GetM(int index)
    {
        throw new NotImplementedException();
    }

    public override Coordinate[] ToCoordinateArray()
    {
        throw new NotImplementedException();
    }

    public override double GetZ(int index)
    {
        throw new NotImplementedException();
    }

    public override void SetM(int index, double value)
    {
        throw new NotImplementedException();
    }

    public override void SetX(int index, double value)
    {
        throw new NotImplementedException();
    }

    public override void SetY(int index, double value)
    {
        throw new NotImplementedException();
    }

    public override void SetZ(int index, double value)
    {
        throw new NotImplementedException();
    }

    #endregion
}