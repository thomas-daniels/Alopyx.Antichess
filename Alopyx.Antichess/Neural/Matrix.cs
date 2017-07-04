using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Alopyx.Antichess.Neural
{
    public class Matrix
    {
        double[][] content;
        public int Rows
        {
            get;
            private set;
        }
        public int Columns
        {
            get;
            private set;
        }

        private Matrix() { }

        public Matrix(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            content = new double[rows][];
            for (int i = 0; i < rows; i++)
            {
                content[i] = new double[columns];
            }
        }

        public void Set(int i, int j, double value)
        {
            content[i - 1][j - 1] = value;
        }

        public double Get(int i, int j)
        {
            return content[i - 1][j - 1];
        }

        public void InitializeRandomly()
        {
            Random rnd = new Random();
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    content[r][c] = rnd.NextDouble();
                }
            }
        }

        public Matrix Multiply(Matrix other)
        {
            if (Columns != other.Rows) throw new InvalidOperationException();

            Matrix result = new Matrix(Rows, other.Columns);
            for (int r = 1; r <= Rows; r++)
            {
                for (int c = 1; c <= other.Columns; c++)
                {
                    result.Set(r, c, MultiplicationOfRowWithColumn(other, r, c));
                }
            }

            return result;
        }

        double MultiplicationOfRowWithColumn(Matrix other, int r, int c)
        {
            double sum = 0;
            for (int a = 1; a <= Columns; a++)
            {
                sum += Get(r, a) * other.Get(a, c);
            }
            return sum;
        }

        public Matrix ApplyOnAllElements(Func<double, double> function)
        {
            Matrix newM = new Matrix(Rows, Columns);
            for (int i = 1; i <= Rows; i++)
            {
                for (int j = 1; j <= Columns; j++)
                {
                    newM.Set(i, j, function(Get(i, j)));
                }
            }
            return newM;
        }

        public Matrix Transpose()
        {
            Matrix t = new Matrix(Columns, Rows);
            for (int i = 1; i <= Rows; i++)
            {
                for (int j = 1; j <= Columns; j++)
                {
                    t.Set(j, i, Get(i, j));
                }
            }
            return t;
        }

        public Matrix Add(Matrix other)
        {
            if (Rows != other.Rows || Columns != other.Columns) throw new InvalidOperationException();

            Matrix newM = new Matrix(Rows, Columns);
            for (int i = 1; i <= Rows; i++)
            {
                for (int j = 1; j <= Columns; j++)
                {
                    newM.Set(i, j, Get(i, j) + other.Get(i, j));
                }
            }
            return newM;
        }

        public Matrix MultiplyOneOnOne(Matrix other)
        {
            if (Rows != other.Rows || Columns != other.Columns) throw new InvalidOperationException();

            Matrix newM = new Matrix(Rows, Columns);
            for (int i = 1; i <= Rows; i++)
            {
                for (int j = 1; j <= Columns; j++)
                {
                    newM.Set(i, j, Get(i, j) * other.Get(i, j));
                }
            }
            return newM;
        }

        public static Matrix Column(double[] list)
        {
            Matrix m = new Matrix(list.Length, 1);
            for (int i = 1; i <= m.Rows; i++)
            {
                m.Set(i, 1, list[i - 1]);
            }
            return m;
        }

        public void Save(string path)
        {
            IEnumerable<string> flattened = content.Select(x => string.Join(",", x));
            string stringified = string.Join(";", flattened);
            File.WriteAllText(path, stringified);
        }

        public static Matrix Load(string path)
        {
            Matrix result = new Matrix();

            string stringified = File.ReadAllText(path);
            string[] rows = stringified.Split(';');
            double[][] content = rows.Select(x => x.Split(',').Select(double.Parse).ToArray()).ToArray();
            result.content = content;
            result.Rows = rows.Length;
            result.Columns = content[0].Length;

            return result;
        }
    }
}
