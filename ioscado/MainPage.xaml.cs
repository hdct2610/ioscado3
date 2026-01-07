using ioscado;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace ioscado
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<Match> Matches { get; set; } = new ObservableCollection<Match>();
        private readonly HttpClient client = new HttpClient();

        // Biến lưu trạng thái Tab (1=Think, 2=Score, 3=Scout)
        private int currentTab = 1;

        public MainPage()
        {
            InitializeComponent();
            MatchList.ItemsSource = Matches;
        }

        // --- XỬ LÝ TAB CLICK (Thay thế Radio Button) ---
        private void OnTabClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            string tabId = btn.CommandParameter.ToString();
            currentTab = int.Parse(tabId);

            // Reset màu các tab về trong suốt
            bgTab1.Color = Colors.Transparent;
            bgTab2.Color = Colors.Transparent;
            bgTab3.Color = Colors.Transparent;

            // Bật màu tab được chọn
            if (currentTab == 1) bgTab1.Color = Color.FromArgb("#2563eb"); // Xanh
            if (currentTab == 2) bgTab2.Color = Color.FromArgb("#2563eb");
            if (currentTab == 3) bgTab3.Color = Color.FromArgb("#2563eb");
        }

        // --- LẤY DỮ LIỆU ---
        private async void OnNhapDuLieuClicked(object sender, EventArgs e)
        {
            if (btnNhapDuLieu == null) return;

            // Khóa nút nhưng KHÔNG đổi text để tránh vỡ layout (theo yêu cầu của bạn)
            btnNhapDuLieu.IsEnabled = false;
            btnNhapDuLieu.Opacity = 0.7; // Làm mờ nhẹ để biết đang bấm

            Matches.Clear();

            try
            {
                var listToday = await GetMatchesForDateAsync(DateTime.Now);
                var listTomorrow = await GetMatchesForDateAsync(DateTime.Now.AddDays(1));

                var allMatches = listToday.Concat(listTomorrow)
                                          .Where(m => m != null && m.CategoryName == "Bóng đá")
                                          .OrderBy(m => m.MatchTimestamp)
                                          .ToList();

                if (allMatches.Count > 0)
                {
                    foreach (var match in allMatches) Matches.Add(match);
                }
                else
                {
                    ShowToast("Không tìm thấy trận nào!");
                }
            }
            catch (Exception ex)
            {
                ShowToast("Lỗi mạng rồi đại ca!");
            }
            finally
            {
                btnNhapDuLieu.IsEnabled = true;
                btnNhapDuLieu.Opacity = 1.0;
            }
        }

        private async Task<List<Match>> GetMatchesForDateAsync(DateTime date)
        {
            string dateString = date.ToString("yyyyMMdd");
            string apiUrl = $"https://json.vnres.co/match/matches_{dateString}.json";

            try
            {
                string jsonpString = await client.GetStringAsync(apiUrl);
                int startIndex = jsonpString.IndexOf('(');
                int endIndex = jsonpString.LastIndexOf(')');

                if (startIndex == -1 || endIndex == -1) return new List<Match>();

                string jsonString = jsonpString.Substring(startIndex + 1, endIndex - startIndex - 1);
                var response = JsonConvert.DeserializeObject<ApiResponse>(jsonString);

                if (response != null && response.Code == 200 && response.Data != null)
                    return response.Data;
            }
            catch { }
            return new List<Match>();
        }

        // --- SAO CHÉP PROMPT (Dùng Logic Tab mới) ---
        private async void OnSaoChepClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var match = button?.CommandParameter as Match;
            if (match == null) return;

            string tranDauText = $"{match.HomeTeam} vs {match.AwayTeam}";
            string giaiText = match.LeagueName ?? "";
            string finalPrompt = "";

            // Logic chọn prompt dựa trên biến currentTab
            if (currentTab == 1) // Deep Research
            {
                finalPrompt = $"Giải {giaiText}. {tranDauText}. Bối cảnh: Bạn là một nhà phân tích bóng đá... (Dữ liệu Deep Research)";
            }
            else if (currentTab == 2) // Score Prediction
            {
                finalPrompt = $"Giải {giaiText}. {tranDauText}, hãy dự đoán tỉ số H1/H2...";
            }
            else if (currentTab == 3) // Scout Bóng cỏ
            {
                finalPrompt = $"Giải {giaiText}. {tranDauText}# VAI TRÒ: Thợ Săn Kèo Bóng Cỏ...";
            }

            await Clipboard.Default.SetTextAsync(finalPrompt);

            // Dùng Toast thay vì DisplayAlert
            ShowToast($"Đã copy: {match.HomeTeam}");
        }

        private async void OnPromptKetHopClicked(object sender, EventArgs e)
        {
            string prompt = @"# VAI TRÒ: Trưởng ban Tình báo Chiến lược Bóng đá...";
            await Clipboard.Default.SetTextAsync(prompt);
            ShowToast("Đã chép Prompt Tổng Hợp!");
        }

        // --- HÀM HIỆN THÔNG BÁO TOAST (Tự chế) ---
        private async void ShowToast(string message)
        {
            if (ToastMessage == null) return;

            ToastText.Text = message;
            ToastMessage.Opacity = 0;
            ToastMessage.TranslationY = 20;

            // Animation hiện lên
            await Task.WhenAll(
                ToastMessage.FadeTo(1, 250, Easing.CubicOut),
                ToastMessage.TranslateTo(0, 0, 250, Easing.CubicOut)
            );

            // Chờ 1.5 giây
            await Task.Delay(1500);

            // Animation ẩn đi
            await Task.WhenAll(
                ToastMessage.FadeTo(0, 250, Easing.CubicIn),
                ToastMessage.TranslateTo(0, 20, 250, Easing.CubicIn)
            );
        }
    }
}