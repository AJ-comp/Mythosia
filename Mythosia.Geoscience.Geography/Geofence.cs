using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Mythosia.Geoscience.Geography
{
    public class Geofence : IEnumerable<GeoCoordinate>, IEnumerable, IReadOnlyCollection<GeoCoordinate>, ICollection
    {
        private HashSet<GeoCoordinate> coordinates;

        public Geofence()
        {
            coordinates = new HashSet<GeoCoordinate>();
        }

        public int Count => coordinates.Count;

        public bool IsSynchronized => throw new NotImplementedException();
        public object SyncRoot => throw new NotImplementedException();

        public void Add(GeoCoordinate coordinate)
        {
            coordinates.Add(coordinate);
        }

        public bool Contains(GeoCoordinate coordinate)
        {
            return coordinates.Contains(coordinate);
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<GeoCoordinate> GetEnumerator()
        {
            return coordinates.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
