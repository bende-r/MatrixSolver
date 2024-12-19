using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MatrixSolverClientForm
{
    public partial class ClientForm : Form
    {
        private string serverUrl; // Server URL loaded from config.json

        private TabControl tabControl;
        private TabPage tabMain;
        private TabPage tabConversion;

        private TextBox txtMatrixFile;
        private TextBox txtVectorFile;
        private TextBox txtCombinedFile;
        private RadioButton rbCombined;
        private RadioButton rbSeparate;
        private Button btnSelectMatrix;
        private Button btnSelectVector;
        private Button btnSelectCombined;
        private Button btnSend;
        private Button btnSaveResult;
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;
        private TextBox txtResult;

        private TextBox txtMatrixAFile;
        private TextBox txtVectorBFile;
        private TextBox txtAnswerDesFile;
        private Button btnSelectAFile;
        private Button btnSelectBFile;
        private Button btnSelectDesFile;
        private Button btnConvertAndCompare;
        private TextBox txtConversionResult;

        public ClientForm()
        {
            LoadConfiguration(); // Load server URL from config.json
            InitializeComponents();
        }

        private void LoadConfiguration()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "../../", "config.json");
                if (!File.Exists(configPath))
                {
                    throw new FileNotFoundException("Файл config.json не найден.");
                }

                string configJson = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, string>>(configJson);

                if (config != null && config.TryGetValue("ServerUrl", out var url))
                {
                    serverUrl = url;
                }
                else
                {
                    throw new InvalidOperationException("URL сервера не найден в файле config.json.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки конфигурации: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                serverUrl = "ws://localhost:5145/ws"; // Default to localhost
            }
        }

        private void InitializeComponents()
        {
            this.Text = "Matrix Solver Client";
            this.Size = new System.Drawing.Size(900, 700);

            tabControl = new TabControl { Dock = DockStyle.Fill };

            tabMain = new TabPage("Основное") { Padding = new Padding(10) };
            tabConversion = new TabPage("Конвертация и проверка") { Padding = new Padding(10) };

            InitializeMainTab();
            InitializeConversionTab();

            tabControl.TabPages.Add(tabMain);
            tabControl.TabPages.Add(tabConversion);

            this.Controls.Add(tabControl);

            // Инициализация OpenFileDialog
            openFileDialog = new OpenFileDialog
            {
                Filter = "All Files|*.*|JSON Files|*.json|Matrix Files|*.A|Vector Files|*.B|Result Files|*.DES",
                FilterIndex = 2,
                RestoreDirectory = true
            };

            saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Files|*.json|All Files|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };
        }

        private void InitializeMainTab()
        {
            Label lblMode = new Label { Text = "Выберите режим загрузки:", Left = 10, Top = 10, Width = 200 };

            rbCombined = new RadioButton { Text = "Один JSON файл", Left = 20, Top = 40, Width = 150, Checked = true };
            rbCombined.CheckedChanged += ToggleMode;

            rbSeparate = new RadioButton { Text = "Отдельные файлы", Left = 200, Top = 40, Width = 150 };

            Label lblMatrixFile = new Label { Text = "Файл матрицы (A):", Left = 10, Top = 80, Width = 150 };
            txtMatrixFile = new TextBox { Left = 170, Top = 80, Width = 400, ReadOnly = true, Enabled = false };
            btnSelectMatrix = new Button { Text = "Обзор", Left = 600, Top = 80, Width = 100, Enabled = false };
            btnSelectMatrix.Click += (sender, e) => SelectFile(txtMatrixFile);

            Label lblVectorFile = new Label { Text = "Файл вектора (B):", Left = 10, Top = 120, Width = 150 };
            txtVectorFile = new TextBox { Left = 170, Top = 120, Width = 400, ReadOnly = true, Enabled = false };
            btnSelectVector = new Button { Text = "Обзор", Left = 600, Top = 120, Width = 100, Enabled = false };
            btnSelectVector.Click += (sender, e) => SelectFile(txtVectorFile);

            Label lblCombinedFile = new Label { Text = "Один JSON файл:", Left = 10, Top = 160, Width = 150 };
            txtCombinedFile = new TextBox { Left = 170, Top = 160, Width = 400, ReadOnly = true };
            btnSelectCombined = new Button { Text = "Обзор", Left = 600, Top = 160, Width = 100 };
            btnSelectCombined.Click += (sender, e) => SelectFile(txtCombinedFile);

            btnSend = new Button { Text = "Отправить", Left = 10, Top = 200, Width = 150 };
            btnSend.Click += BtnSend_Click;

            btnSaveResult = new Button { Text = "Сохранить результат", Left = 200, Top = 200, Width = 150, Enabled = false };
            btnSaveResult.Click += BtnSaveResult_Click;

            txtResult = new TextBox { Left = 10, Top = 250, Width = 750, Height = 250, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

            tabMain.Controls.Add(lblMode);
            tabMain.Controls.Add(rbCombined);
            tabMain.Controls.Add(rbSeparate);
            tabMain.Controls.Add(lblMatrixFile);
            tabMain.Controls.Add(txtMatrixFile);
            tabMain.Controls.Add(btnSelectMatrix);
            tabMain.Controls.Add(lblVectorFile);
            tabMain.Controls.Add(txtVectorFile);
            tabMain.Controls.Add(btnSelectVector);
            tabMain.Controls.Add(lblCombinedFile);
            tabMain.Controls.Add(txtCombinedFile);
            tabMain.Controls.Add(btnSelectCombined);
            tabMain.Controls.Add(btnSend);
            tabMain.Controls.Add(btnSaveResult);
            tabMain.Controls.Add(txtResult);
        }

        private void ToggleMode(object sender, EventArgs e)
        {
            bool isCombined = rbCombined.Checked;
            txtMatrixFile.Enabled = !isCombined;
            btnSelectMatrix.Enabled = !isCombined;
            txtVectorFile.Enabled = !isCombined;
            btnSelectVector.Enabled = !isCombined;
            txtCombinedFile.Enabled = isCombined;
            btnSelectCombined.Enabled = isCombined;
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            string combinedFile = txtCombinedFile.Text;
            string matrixFile = txtMatrixFile.Text;
            string vectorFile = txtVectorFile.Text;

            string jsonData;
            if (rbCombined.Checked)
            {
                if (string.IsNullOrWhiteSpace(combinedFile) || !File.Exists(combinedFile))
                {
                    MessageBox.Show("Выберите существующий файл.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                jsonData = File.ReadAllText(combinedFile);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(matrixFile) || !File.Exists(matrixFile) ||
                    string.IsNullOrWhiteSpace(vectorFile) || !File.Exists(vectorFile))
                {
                    MessageBox.Show("Выберите оба файла.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var data = new
                {
                    Matrix = JsonSerializer.Deserialize<double[][]>(File.ReadAllText(matrixFile)),
                    Vector = JsonSerializer.Deserialize<double[]>(File.ReadAllText(vectorFile))
                };

                jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            }

            await SendToServer(jsonData);
        }

        private async Task SendToServer(string jsonData)
        {
            try
            {
                ClientWebSocket client = new ClientWebSocket();
                using (client)
                {
                    txtResult.Text = "Подключение к серверу...\r\n";
                    await client.ConnectAsync(new Uri("ws://localhost:5145/ws"), CancellationToken.None);
                    txtResult.AppendText("Соединение установлено.\r\n");

                    byte[] messageBytes = Encoding.UTF8.GetBytes(jsonData);
                    await client.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    var buffer = new ArraySegment<byte>(new byte[4096]);
                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await client.ReceiveAsync(buffer, CancellationToken.None);
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        } while (!result.EndOfMessage);

                        string response = Encoding.UTF8.GetString(ms.ToArray());
                        txtResult.AppendText("Ответ от сервера: \r\n" + response);
                        btnSaveResult.Enabled = true;

                        // Сохраняем результат для возможного сохранения
                        LastServerResponse = response;
                    }

                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Клиент завершил работу", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string LastServerResponse;

        private void BtnSaveResult_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, LastServerResponse);
                MessageBox.Show("Результат успешно сохранён.", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void InitializeConversionTab()
        {
            Label lblMatrixAFile = new Label { Text = "Файл матрицы (.A):", Left = 10, Top = 10, Width = 150 };
            txtMatrixAFile = new TextBox { Left = 170, Top = 10, Width = 400, ReadOnly = true };
            btnSelectAFile = new Button { Text = "Обзор", Left = 600, Top = 10, Width = 100 };
            btnSelectAFile.Click += (sender, e) => SelectFile(txtMatrixAFile);

            Label lblVectorBFile = new Label { Text = "Файл вектора (.B):", Left = 10, Top = 50, Width = 150 };
            txtVectorBFile = new TextBox { Left = 170, Top = 50, Width = 400, ReadOnly = true };
            btnSelectBFile = new Button { Text = "Обзор", Left = 600, Top = 50, Width = 100 };
            btnSelectBFile.Click += (sender, e) => SelectFile(txtVectorBFile);

            Label lblAnswerDesFile = new Label { Text = "Файл ответов (.DES):", Left = 10, Top = 90, Width = 150 };
            txtAnswerDesFile = new TextBox { Left = 170, Top = 90, Width = 400, ReadOnly = true };
            btnSelectDesFile = new Button { Text = "Обзор", Left = 600, Top = 90, Width = 100 };
            btnSelectDesFile.Click += (sender, e) => SelectFile(txtAnswerDesFile);

            btnConvertAndCompare = new Button { Text = "Конвертировать и сравнить", Left = 10, Top = 130, Width = 200 };
            btnConvertAndCompare.Click += BtnConvertAndCompare_Click;

            txtConversionResult = new TextBox { Left = 10, Top = 180, Width = 750, Height = 250, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

            tabConversion.Controls.Add(lblMatrixAFile);
            tabConversion.Controls.Add(txtMatrixAFile);
            tabConversion.Controls.Add(btnSelectAFile);
            tabConversion.Controls.Add(lblVectorBFile);
            tabConversion.Controls.Add(txtVectorBFile);
            tabConversion.Controls.Add(btnSelectBFile);
            tabConversion.Controls.Add(lblAnswerDesFile);
            tabConversion.Controls.Add(txtAnswerDesFile);
            tabConversion.Controls.Add(btnSelectDesFile);
            tabConversion.Controls.Add(btnConvertAndCompare);
            tabConversion.Controls.Add(txtConversionResult);
        }

        private void SelectFile(TextBox textBox)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox.Text = openFileDialog.FileName;
            }
        }

        public class ServerResponse
        {
            public double[] StripeSolution { get; set; }
        }


        private async void BtnConvertAndCompare_Click(object sender, EventArgs e)
        {
            string matrixAFile = txtMatrixAFile.Text;
            string vectorBFile = txtVectorBFile.Text;
            string answerDesFile = txtAnswerDesFile.Text;

            if (string.IsNullOrWhiteSpace(matrixAFile) || !File.Exists(matrixAFile) ||
                string.IsNullOrWhiteSpace(vectorBFile) || !File.Exists(vectorBFile) ||
                string.IsNullOrWhiteSpace(answerDesFile) || !File.Exists(answerDesFile))
            {
                MessageBox.Show("Выберите все необходимые файлы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                txtConversionResult.AppendText("Чтение файлов .A и .B...\r\n");

                // Конвертация файлов .A и .B в JSON
                double[][] matrix;
                double[] vector;

                try
                {
                    // Stream-based reading for large .A files
                    matrix = await ReadMatrixFromLargeFileAsync(matrixAFile);
                    txtConversionResult.AppendText("Файл .A успешно прочитан.\r\n");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Ошибка при чтении файла .A: {ex.Message}");
                }

                try
                {
                    vector = File.ReadAllLines(vectorBFile)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .Select(value => double.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture)) // Учитываем формат чисел
                        .ToArray();
                    txtConversionResult.AppendText("Файл .B успешно прочитан.\r\n");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Ошибка при чтении файла .B: {ex.Message}");
                }

                var data = new { Matrix = matrix, Vector = vector };
                string jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

                txtConversionResult.AppendText("Отправка данных на сервер...\r\n");

                // Отправка на сервер
                string serverResponse;
                try
                {
                    serverResponse = await SendAndReceive(jsonData);
                    txtConversionResult.AppendText("Данные успешно отправлены на сервер.\r\n");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Ошибка при отправке данных на сервер: {ex.Message}");
                }

                txtConversionResult.AppendText("Обработка ответа сервера...\r\n");

                // Чтение файла .DES
                double[] expectedAnswers;
                try
                {
                    expectedAnswers = File.ReadAllLines(answerDesFile)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .Select(value => double.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture)) // Учитываем формат чисел
                        .ToArray();
                    txtConversionResult.AppendText("Файл .DES успешно прочитан.\r\n");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Ошибка при чтении файла .DES: {ex.Message}");
                }

                double[] actualAnswers;
                try
                {
                    var response = JsonSerializer.Deserialize<ServerResponse>(serverResponse);

                    if (response?.StripeSolution == null)
                        throw new Exception("Ответ сервера не содержит решения StripeSolution.");

                    actualAnswers = response.StripeSolution;
                    txtConversionResult.AppendText("Ответ сервера успешно обработан.\r\n");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Ошибка при десериализации ответа сервера: {ex.Message}\r\nОтвет сервера:\r\n{serverResponse}");
                }

                // Сравнение результатов с детальным анализом
                txtConversionResult.Text = "Сравнение результатов:\r\n";
                if (expectedAnswers.Length != actualAnswers.Length)
                {
                    txtConversionResult.AppendText("Результаты НЕ совпадают. Различная длина массивов.\r\n");
                    txtConversionResult.AppendText($"Ожидаемая длина: {expectedAnswers.Length}, Фактическая длина: {actualAnswers.Length}\r\n");
                }
                else
                {
                    bool isEqual = true;
                    for (int i = 0; i < expectedAnswers.Length; i++)
                    {
                        // Учет допустимой погрешности
                        if (Math.Abs(expectedAnswers[i] - actualAnswers[i]) > 1e-4)
                        {
                            isEqual = false;
                            txtConversionResult.AppendText($"Разница на индексе {i}: Ожидаемое = {expectedAnswers[i]}, Фактическое = {actualAnswers[i]}\r\n");
                        }
                    }

                    if (isEqual)
                    {
                        txtConversionResult.AppendText("Результаты совпадают.\r\n");
                    }
                    else
                    {
                        txtConversionResult.AppendText("Результаты НЕ совпадают.\r\n");
                    }
                }

                // Вывод полного ответа сервера и ожидаемых значений
                txtConversionResult.AppendText("\r\nОтвет сервера:\r\n" + string.Join(", ", actualAnswers) + "\r\n");
                txtConversionResult.AppendText("Ожидаемые ответы:\r\n" + string.Join(", ", expectedAnswers));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtConversionResult.AppendText($"Детали ошибки: {ex.Message}\r\n");
            }
        }

        // Метод для чтения матрицы из большого файла построчно
        private async Task<double[][]> ReadMatrixFromLargeFileAsync(string filePath)
        {
            var matrix = new List<double[]>();
            using (var streamReader = new StreamReader(filePath))
            {
                string line;
                while ((line = await streamReader.ReadLineAsync()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var row = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(value => double.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture))
                                      .ToArray();
                        matrix.Add(row);
                    }
                }
            }
            return matrix.ToArray();
        }

        private async Task<string> SendAndReceive(string jsonData)
        {
            try
            {
                ClientWebSocket client = new ClientWebSocket();
                using (client)
                {
                    txtConversionResult.Text = "Подключение к серверу...\r\n";
                    await client.ConnectAsync(new Uri("ws://localhost:5145/ws"), CancellationToken.None);

                    byte[] messageBytes = Encoding.UTF8.GetBytes(jsonData);
                    await client.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    var buffer = new ArraySegment<byte>(new byte[4096]);
                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await client.ReceiveAsync(buffer, CancellationToken.None);
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        } while (!result.EndOfMessage);

                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при отправке данных на сервер: " + ex.Message);
            }
        }
    }
}