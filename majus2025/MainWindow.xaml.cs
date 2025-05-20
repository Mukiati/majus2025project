using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace majus2025
{
    public partial class MainWindow : Window
    {
        private const string BaseUrl = "http://localhost:5555";
        private string jwtToken = null;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += async (_, __) =>
            {
                await LoadQuizzes();
            };
        }


        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            tbLoginMessage.Text = "";

            if (string.IsNullOrWhiteSpace(tbUsername.Text) || string.IsNullOrWhiteSpace(pbPassword.Password))
            {
                tbLoginMessage.Foreground = System.Windows.Media.Brushes.LightCoral;
                tbLoginMessage.Text = "Töltsd ki a felhasználónevet és a jelszót!";
                return;
            }

            var userData = new
            {
                username = tbUsername.Text,
                password = pbPassword.Password
            };

            try
            {
                using HttpClient client = new();
                string jsonData = JsonConvert.SerializeObject(userData);
                StringContent content = new(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync($"{BaseUrl}/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResp = await response.Content.ReadAsStringAsync();
                    dynamic respObj = JsonConvert.DeserializeObject(jsonResp);
                    jwtToken = respObj.token;

                    tbLoginMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                    tbLoginMessage.Text = "Sikeres bejelentkezés!";

                    await LoadQuizzes();
                }
                else
                {
                    string errMsg = await response.Content.ReadAsStringAsync();
                    tbLoginMessage.Foreground = System.Windows.Media.Brushes.LightCoral;
                    tbLoginMessage.Text = $"Hiba: {errMsg}";
                }
            }
            catch (Exception ex)
            {
                tbLoginMessage.Foreground = System.Windows.Media.Brushes.LightCoral;
                tbLoginMessage.Text = "Hiba: " + ex.Message;
            }
        }


        private async void BtnRegister_Click(object sender, EventArgs e)
        {
            tbLoginMessage.Text = "";

            if (string.IsNullOrWhiteSpace(tbUsername.Text) || string.IsNullOrWhiteSpace(pbPassword.Password))
            {
                tbLoginMessage.Foreground = System.Windows.Media.Brushes.LightCoral;
                tbLoginMessage.Text = "Töltsd ki a felhasználónevet és a jelszót!";
                return;
            }

            var userData = new
            {
                username = tbUsername.Text,
                password = pbPassword.Password
            };

            try
            {
                using HttpClient client = new();
                string jsonData = JsonConvert.SerializeObject(userData);
                StringContent content = new(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync($"{BaseUrl}/users/register", content);

                if (response.IsSuccessStatusCode)
                {
                    tbLoginMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                    tbLoginMessage.Text = "Sikeres regisztráció!";
                }
                else
                {
                    string errMsg = await response.Content.ReadAsStringAsync();
                    tbLoginMessage.Foreground = System.Windows.Media.Brushes.LightCoral;
                    tbLoginMessage.Text = $"Hiba: {errMsg}";
                }
            }
            catch (Exception ex)
            {
                tbLoginMessage.Foreground = System.Windows.Media.Brushes.LightCoral;
                tbLoginMessage.Text = "Hiba: " + ex.Message;
            }
        }


        public class Quiz
        {
            public int id { get; set; }
            public string title { get; set; }
            public string description { get; set; }
        }


        private async Task LoadQuizzes(string searchTerm = null)
        {
            if (string.IsNullOrEmpty(jwtToken))
            {
                spQuizzes.Children.Clear();
                return;
            }

            try
            {
                using HttpClient client = new();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

                HttpResponseMessage response = await client.GetAsync($"{BaseUrl}/quizzes");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    List<Quiz> quizzes = JsonConvert.DeserializeObject<List<Quiz>>(json);

                    // Szűrés, ha van keresési kifejezés
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        quizzes = quizzes.FindAll(q => q.title.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
                    }

                    spQuizzes.Children.Clear();

                    foreach (var quiz in quizzes)
                    {
                        Border border = new Border()
                        {
                            Background = System.Windows.Media.Brushes.DimGray,
                            CornerRadius = new CornerRadius(8),
                            Margin = new Thickness(5),
                            Padding = new Thickness(10),
                        };

                        StackPanel sp = new StackPanel();

                        TextBlock tbTitle = new TextBlock()
                        {
                            Text = $"Cím: {quiz.title}",
                            Foreground = System.Windows.Media.Brushes.White,
                            FontWeight = FontWeights.Bold,
                            FontSize = 14
                        };
                        TextBlock tbDesc = new TextBlock()
                        {
                            Text = $"Leírás: {quiz.description}",
                            Foreground = System.Windows.Media.Brushes.LightGray,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 5, 0, 5)
                        };

                        Button btnDetails = new Button() { Content = "Részletek", Width = 70, Margin = new Thickness(0, 0, 5, 0) };
                        Button btnEdit = new Button() { Content = "Szerkesztés", Width = 70, Margin = new Thickness(0, 0, 5, 0) };
                        Button btnDelete = new Button() { Content = "Törlés", Width = 70 };


                        btnDetails.Click += (s, e) =>
                        {
                            MessageBox.Show($"Kvíz részletei:\n\nCím: {quiz.title}\nLeírás: {quiz.description}", "Kvíz részletek");
                        };

                        btnEdit.Click += async (s, e) =>
                        {
                            string newTitle = Microsoft.VisualBasic.Interaction.InputBox("Új cím:", "Kvíz szerkesztése", quiz.title);
                            if (string.IsNullOrWhiteSpace(newTitle)) return;
                            string newDesc = Microsoft.VisualBasic.Interaction.InputBox("Új leírás:", "Kvíz szerkesztése", quiz.description);

                            var quizData = new { title = newTitle, description = newDesc };

                            try
                            {
                                string jsonData = JsonConvert.SerializeObject(quizData);
                                StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                                using HttpClient client = new();
                                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

                                HttpResponseMessage putResp = await client.PutAsync($"{BaseUrl}/quizzes/{quiz.id}", content);

                                if (putResp.IsSuccessStatusCode)
                                {
                                    MessageBox.Show("Kvíz frissítve!");
                                    await LoadQuizzes();
                                }
                                else
                                {
                                    MessageBox.Show($"Hiba a frissítéskor: {await putResp.Content.ReadAsStringAsync()}");
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Hiba: " + ex.Message);
                            }
                        };


                        btnDelete.Click += async (s, e) =>
                        {
                            if (MessageBox.Show("Biztos törlöd a kvízt?", "Megerősítés", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                try
                                {
                                    using HttpClient client = new();
                                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

                                    HttpResponseMessage delResp = await client.DeleteAsync($"{BaseUrl}/quizzes/{quiz.id}");
                                    if (delResp.IsSuccessStatusCode)
                                    {
                                        MessageBox.Show("Kvíz törölve.");
                                        await LoadQuizzes();
                                    }
                                    else
                                    {
                                        MessageBox.Show($"Hiba a törléskor: {await delResp.Content.ReadAsStringAsync()}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Hiba: " + ex.Message);
                                }
                            }
                        };

                        StackPanel spButtons = new StackPanel() { Orientation = Orientation.Horizontal };
                        spButtons.Children.Add(btnDetails);
                        spButtons.Children.Add(btnEdit);
                        spButtons.Children.Add(btnDelete);

                        sp.Children.Add(tbTitle);
                        sp.Children.Add(tbDesc);
                        sp.Children.Add(spButtons);

                        border.Child = sp;

                        spQuizzes.Children.Add(border);
                    }
                }
                else
                {
                    MessageBox.Show("Hiba a kvízek lekérésekor: " + await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba: " + ex.Message);
            }
        }


        private async void BtnCreateQuiz_Click(object sender, EventArgs e)
        {
            string title = tbQuizTitle.Text.Trim();
            string description = tbQuizDescription.Text.Trim();

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(description))
            {
                MessageBox.Show("Töltsd ki a címet és a leírást!");
                return;
            }

            var newQuiz = new { title, description };

            try
            {
                using HttpClient client = new();

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

                string jsonData = JsonConvert.SerializeObject(newQuiz);
                StringContent content = new(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync($"{BaseUrl}/quizzes", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Kvíz létrehozva!");
                    tbQuizTitle.Text = "";







                    tbQuizDescription.Text = "";
                    await LoadQuizzes();
                }
                else
                {
                    MessageBox.Show("Hiba: " + await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba: " + ex.Message);
            }
        }


        private async void BtnSearch_Click(object sender, EventArgs e)
        {
            string searchTerm = tbSearchTitle.Text.Trim();
            await LoadQuizzes(searchTerm);
        }
    }
}
