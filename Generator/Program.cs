using System;
using System.Globalization;
using System.IO;
using System.Text.Json;


namespace Generator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            int size = 3000; // Размерность матрицы и вектора
            string projectRootPath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName;

            // Пути для сохранения матрицы A и вектора B
            string matrixFilePath = Path.Combine(projectRootPath, "a3000.json");
            string vectorFilePath = Path.Combine(projectRootPath, "b3000.json");

            // Генерация матрицы и вектора
            double[,] matrix = GenerateMatrix(size);
            double[] vector = GenerateVector(size);

            // Сохранение матрицы и вектора в JSON файлы
            SaveMatrixToJson(matrix, matrixFilePath);
            SaveVectorToJson(vector, vectorFilePath);

            Console.WriteLine("Генерация завершена. Файлы сохранены.");
        }

        // Генерация матрицы коэффициентов A размером size x size, исключая нули
        private static double[,] GenerateMatrix(int size)
        {
            double[,] matrix = new double[size, size];
            Random rand = new Random();

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    double value = 0;
                    // Генерация значений до тех пор, пока не получится ненулевое значение
                    while (value == 0)
                    {
                        value = rand.NextDouble() * 100 - 50; // Значения от -50 до 50
                    }
                    matrix[i, j] = value;
                }
            }

            return matrix;
        }

        // Генерация вектора правых частей B размером size, исключая нули
        private static double[] GenerateVector(int size)
        {
            double[] vector = new double[size];
            Random rand = new Random();

            for (int i = 0; i < size; i++)
            {
                double value = 0;
                // Генерация значений до тех пор, пока не получится ненулевое значение
                while (value == 0)
                {
                    value = rand.NextDouble() * 100 - 50; // Значения от -50 до 50
                }
                vector[i] = value;
            }

            return vector;
        }

        // Сохранение матрицы в JSON файл
        private static void SaveMatrixToJson(double[,] matrix, string filePath)
        {
            int rowCount = matrix.GetLength(0);
            int colCount = matrix.GetLength(1);

            double[][] matrixData = new double[rowCount][];
            for (int i = 0; i < rowCount; i++)
            {
                matrixData[i] = new double[colCount];
                for (int j = 0; j < colCount; j++)
                {
                    matrixData[i][j] = matrix[i, j];
                }
            }

            string json = JsonSerializer.Serialize(matrixData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        // Сохранение вектора в JSON файл
        private static void SaveVectorToJson(double[] vector, string filePath)
        {
            string json = JsonSerializer.Serialize(vector, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
