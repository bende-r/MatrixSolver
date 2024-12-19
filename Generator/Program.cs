using System;
using System.IO;
using System.Text.Json;

namespace Generator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            int size = 228; // Размерность матрицы и вектора
            string projectRootPath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName;

            // Пути для сохранения матрицы A и вектора B
            string matrixFilePath = Path.Combine(projectRootPath, "a228.json");
            string vectorFilePath = Path.Combine(projectRootPath, "b228.json");

            // Сохранение матрицы и вектора в JSON файлы
            SaveMatrixToJson(size, matrixFilePath);
            SaveVectorToJson(size, vectorFilePath);

            Console.WriteLine("Генерация завершена. Файлы сохранены.");
        }

        // Генерация и сохранение матрицы непосредственно в JSON файл
        private static void SaveMatrixToJson(int size, string filePath)
        {
            Random rand = new Random();

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("["); // Начало JSON массива
                for (int i = 0; i < size; i++)
                {
                    writer.Write("[");
                    for (int j = 0; j < size; j++)
                    {
                        double value;
                        do
                        {
                            value = rand.NextDouble() * 100 - 50; // Значения от -50 до 50
                        } while (value == 0);

                        writer.Write(value.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
                        if (j < size - 1)
                        {
                            writer.Write(", ");
                        }
                    }
                    writer.Write("]");
                    if (i < size - 1)
                    {
                        writer.WriteLine(",");
                    }
                }
                writer.WriteLine("]"); // Конец JSON массива
            }
        }

        // Генерация и сохранение вектора непосредственно в JSON файл
        private static void SaveVectorToJson(int size, string filePath)
        {
            Random rand = new Random();

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("["); // Начало JSON массива
                for (int i = 0; i < size; i++)
                {
                    double value;
                    do
                    {
                        value = rand.NextDouble() * 100 - 50; // Значения от -50 до 50
                    } while (value == 0);

                    writer.Write(value.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
                    if (i < size - 1)
                    {
                        writer.WriteLine(",");
                    }
                }
                writer.WriteLine("]"); // Конец JSON массива
            }
        }
    }
}
