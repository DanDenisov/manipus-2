﻿using System;
using System.Linq;

namespace Logic
{
    public struct Vector
    {
        private float[] Components { get; set; }

        public float this[int index] => Components[index];

        public int Size => Components.Length;

        public float Length => (float)Math.Sqrt(Components.Sum(x => x * x));

        public float LengthSquared => Components.Sum(x => x * x);

        public Vector Normalized => this / Length;

        public Vector(int size)
        {
            Components = new float[size];
        }

        public Vector(params float[] components)
        {
            Components = components;
        }

        public void Expand(int size)
        {
            Components = Components.Concat(new float[size]).ToArray();
        }

        public static float Dot(Vector v1, Vector v2)
        {
            return v1.Components.Zip(v2.Components, (x, y) => x * y).Sum();
        }

        public static Vector operator +(Vector v1, Vector v2)
        {
            return new Vector(v1.Components.Zip(v2.Components, (x, y) => x + y).ToArray());
        }

        public static Vector operator -(Vector v1, Vector v2)
        {
            return new Vector(v1.Components.Zip(v2.Components, (x, y) => x - y).ToArray());
        }

        public static Vector operator *(Vector v, float s)
        {
            return new Vector(Array.ConvertAll(v.Components, x => x * s));
        }

        public static Vector operator /(Vector v, float s)
        {
            return new Vector(Array.ConvertAll(v.Components, x => x / s));
        }

        public static Vector operator *(Matrix m, Vector v)  // TODO: optimize
        {
            float[] components = new float[m.RowsNumber];
            for (int i = 0; i < components.Length; i++)
            {
                components[i] = Dot(m.Rows[i], v);
            }

            return new Vector(components);
        }

        private static readonly string ListSeparator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        public override string ToString() => string.Format("({0})", string.Join($"{ListSeparator} ", Components.Select(x => string.Format("{0:#.###}", x))));
    }
}
