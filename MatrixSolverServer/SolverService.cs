﻿using System;
using System.Threading.Tasks;

using static MatrixSolverServer.Program;

namespace MatrixSolverServer
{

    // Реализация сервиса
    public class SolverService : ISolverService
    {
        public double[] SolveSLAUWithStripeMultiplication(double[][] matrix, double[] vector)
        {
            ValidateMatrix(matrix, vector);

            int n = matrix.Length;
            double[] solution = new double[n];

            for (int k = 0; k < n - 1; k++)
            {
                if (Math.Abs(matrix[k][k]) < 1e-10)
                    throw new ArgumentException("Нулевой элемент на диагонали. Решение невозможно.");

                Parallel.For(k + 1, n, i =>
                {
                    double factor = matrix[i][k] / matrix[k][k];
                    for (int j = k; j < n; j++)
                    {
                        matrix[i][j] -= factor * matrix[k][j];
                    }
                    vector[i] -= factor * vector[k];
                });
            }

            for (int i = n - 1; i >= 0; i--)
            {
                solution[i] = vector[i];
                for (int j = i + 1; j < n; j++)
                {
                    solution[i] -= matrix[i][j] * solution[j];
                }
                solution[i] /= matrix[i][i];
            }

            return solution;
        }



        public void ValidateMatrixRequest(MatrixRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("Запрос не должен быть null.");

            ValidateMatrix(request.Matrix, request.Vector);
        }

        private void ValidateMatrix(double[][] matrix, double[] vector)
        {
            if (matrix == null || vector == null)
                throw new ArgumentNullException("Матрица и вектор не должны быть null.");

            int n = matrix.Length;
            if (n == 0 || vector.Length != n)
                throw new ArgumentException("Размеры матрицы и вектора не совпадают или матрица пуста.");

            foreach (var row in matrix)
            {
                if (row.Length != n)
                    throw new ArgumentException("Матрица должна быть квадратной.");
            }

            if (IsInconsistent(matrix, vector))
                throw new ArgumentException("Система несовместна.");
        }

        private static bool IsInconsistent(double[][] matrix, double[] vector)
        {
            int n = matrix.Length;
            for (int i = 0; i < n; i++)
            {
                if (IsZeroRow(matrix[i]) && Math.Abs(vector[i]) > 1e-10)
                {
                    return true; // несовместимая система
                }
            }
            return false;
        }

        private static bool IsZeroRow(double[] row)
        {
            return row.All(val => Math.Abs(val) < 1e-10);
        }
    }
}

