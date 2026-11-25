# Qz • Chinh Phục Tiếng Trung HSK 1 ~ 6 & Luyện Tập Từ Vựng (WPF C# & SQL Server)

Ứng dụng desktop **Qz** được xây dựng bằng công nghệ **WPF (.NET 9 C#)** kết hợp cơ sở dữ liệu **Microsoft SQL Server Localhost**, áp dụng mô hình chuẩn kiến trúc **MVVM (Model - View - ViewModel)**.

Ứng dụng cung cấp **Trang tổng quan (Dashboard)** lộ trình học tiếng Trung bài bản theo **HSK 1 đến HSK 6**, tích hợp nạp tự động toàn bộ hơn **5,000 từ vựng** từ file Excel `11.LUYỆN GÕ HSK 1~6 new.xlsx` vào cơ sở dữ liệu localhost, đồng thời hỗ trợ luyện thi trắc nghiệm theo đúng giao diện mẫu và chế độ luyện gõ câu ví dụ.

---

## 🌟 Các Tính Năng Nổi Bật

### 1. 🇨🇳 Trang Tổng Quan Lộ Trình Học Tiếng Trung (HSK Dashboard)
- **Hero Banner Truyền Cảm Hứng**: Thống kê nhanh tổng từ vựng (**5,002 từ**), số từ đã thuộc, chuỗi ngày học liên tiếp (**Streak 🔥**).
- **Mục Lục & Lộ Trình HSK 1 ~ 6 Bài Bản**:
  - **HSK 1 (🌱 149 từ)**: Khởi đầu sơ cấp - Chào hỏi, số đếm, gia đình, thời gian...
  - **HSK 2 (🌿 150 từ)**: Giao tiếp thường nhật - Mua sắm, hỏi đường, ăn uống, sở thích...
  - **HSK 3 (🎋 295 từ)**: Tự tin đàm thoại - Công việc, du lịch, cảm xúc, biểu đạt suy nghĩ...
  - **HSK 4 (🌸 600 từ)**: Trung cấp thành thạo - Thảo luận chủ đề xã hội, văn phòng, văn hóa...
  - **HSK 5 (🏯 1,295 từ)**: Đọc hiểu & Luận bàn - Báo chí, thuyết trình, phim ảnh chuyên sâu...
  - **HSK 6 (🐉 2,513 từ)**: Tinh hoa cao cấp - Thành ngữ, văn bản học thuật, giao tiếp như người bản xứ.
- Mỗi cấp độ có **thanh tiến độ học**, số từ đã thuộc và các nút hành động: **🎯 Luyện tập trắc nghiệm**, **⌨ Luyện gõ**, **📖 Xem từ vựng**.

### 2. 🗄 Nạp Dữ Liệu Từ Excel Vào SQL Server Localhost
- **Nguồn dữ liệu**: File `C:\Users\Tuan Dat PC\Downloads\11.LUYỆN GÕ HSK 1~6 new.xlsx`.
- **Cơ sở dữ liệu**: Bảng `ChineseVocabularies` trong CSDL `QuizDb` trên SQL Server localhost (`Server=.`).
- Sử dụng công nghệ **`SqlBulkCopy`** kết hợp **`ExcelDataReader`** cho tốc độ nạp siêu tốc: **5,002 từ vựng nạp xong chỉ trong 1-2 giây**.
- Hỗ trợ nút **"🔄 Đồng bộ lại từ Excel"** trực tiếp trên giao diện để cập nhật lại dữ liệu bất cứ lúc nào.

### 3. 🎯 Luyện Tập Trắc Nghiệm Đợt 20 Câu & Vòng Ôn Tập Câu Sai (Sư Phạm Chuẩn)
- **Quản lý theo đợt 20 câu**: Mỗi lượt làm bài trắc nghiệm gồm chính xác **20 từ vựng**.
- **Vòng hỏi lại câu sai (Review Round)**:
  - Khi người dùng trả lời xong 20 câu ban đầu, nếu có bất kỳ câu nào chọn sai, ứng dụng **sẽ không kết thúc ngay** mà tự động chuyển sang **Vòng ôn tập câu sai**.
  - Người dùng được hỏi lại toàn bộ các câu đã làm sai cho đến khi trả lời đúng để khắc sâu kiến thức.
- **Đánh giá điểm số theo điều kiện 70%**:
  - **Dưới 70% (< 14/20 câu đúng ở lượt đầu)**:
    - Bắt buộc phải thi lại lượt 20 từ này, không cho phép mở 20 từ tiếp theo.
    - Nút hành động chính: `🔄 Bắt buộc thi lại 20 câu này ➔`.
  - **Từ 70% trở lên (≥ 14/20 câu đúng ở lượt đầu)**:
    - Đạt chuẩn! Toàn bộ 20 từ đã hoàn thành được **lưu trực tiếp vào CSDL localhost** (`PracticeCount + 1`, `IsMastered = 1`).
    - Cho người dùng quyền lựa chọn:
      - `🚀 Tiếp tục với 20 từ tiếp theo ➔`: Bắt đầu đợt mới với 20 từ tiếp theo chưa làm từ CSDL!
      - `🏠 Dừng lại & Về trang chính`: Quay về trang tổng quan, tiến độ học được cập nhật ngay lập tức.

### 4. ⌨ Phòng Luyện Gõ HSK (Theo Chuẩn File Excel)
- Bám sát cấu trúc của file Excel luyện gõ:
  - Ô 1: **Luyện gõ từ mới** (Chữ Hán hoặc Pinyin) ➔ Tự động kiểm tra đúng/sai tức thì (Tick xanh ✓).
  - Ô 2: **Luyện gõ cả câu ví dụ** ➔ Đánh giá độ chính xác khi gõ câu.
- Nút **"💡 Gợi ý / Tự điền"** và tự động ghi nhận số lần luyện tập vào cơ sở dữ liệu.

### 5. 📚 Sổ Tay Từ Vựng & Tra Cứu HSK
- Tra cứu danh sách từ vựng theo từng cấp độ HSK 1 đến 6 hoặc toàn bộ.
- Tìm kiếm tức thì theo chữ Hán, Pinyin hoặc nghĩa tiếng Việt.
- Nút **"✓ Đánh dấu đã thuộc"** cập nhật trực tiếp vào SQL Server để theo dõi tiến độ thực tế.

### 6. Luyện nói tiếng Trung với FunASR

- Mở **Luyện nói AI**, bấm **Thu âm**, đọc từ hoặc câu tiếng Trung, rồi bấm **Dừng thu âm**. Kết quả xuất hiện trong ô **Văn bản nhận dạng** và có thể sao chép. Nút **Nghe lại giọng của bạn** phát lại bản thu gần nhất.
- Ứng dụng tải mô hình FunASR Paraformer bản ONNX INT8 khoảng 243 MB khi xử lý bản thu có âm thanh đầu tiên. Mô hình được lưu ở `%LOCALAPPDATA%\QuizApp\FunASR\paraformer-zh-2023-09-14`. Các lần nhận dạng sau chạy trên CPU bằng sherpa-onnx ngay trên máy. [Mô hình Paraformer](https://k2-fsa.github.io/sherpa/onnx/pretrained_models/offline-paraformer/paraformer-models.html)
- Chọn **Đọc từ** hoặc **Đọc cả câu**. Ứng dụng thu từ microphone mặc định của Windows qua WASAPI và ưu tiên chế độ xử lý giọng nói nếu thiết bị hỗ trợ. Bộ lọc giảm tiếng ù và tiếng nền đều, rồi cắt khoảng chờ ở hai đầu. Nút **Nghe lại giọng của bạn** phát bản đã lọc và cân âm lượng. Nếu chưa tạo được bản lọc, ứng dụng hiện nút **Nghe bản thu gốc**. Mỗi bản thu dài tối đa 30 giây.
- Ứng dụng dùng thêm [Zipformer tiếng Trung](https://huggingface.co/csukuangfj/sherpa-onnx-zipformer-ctc-small-zh-int8-2025-07-16) khoảng 63 MB cho bài đọc từ ngắn hoặc khi FunASR cho kết quả khác nhau giữa bản gốc và bản lọc. Mục **Văn bản nhận dạng** ưu tiên phương án khớp với âm, thanh điệu và các mô hình, đồng thời báo khi chữ nhận dạng còn chưa chắc. Dòng **Âm nghe được** hiển thị Pinyin do mô hình âm vị nhận ra từ bản thu, kể cả khi âm đó khác mẫu trên thẻ.
- Bộ chấm dùng [Mandarin IPA ASR](https://github.com/chenhongwen2005/mandarin-ipa-asr) và đo F0 bằng [Praat qua Parselmouth](https://parselmouth.readthedocs.io/). Mô hình IPA có giấy phép **CC BY-NC-SA 4.0** và được dùng trong Qz để học tập phi thương mại. Bản thu được xử lý trên máy.
- Trên máy mới, cài Python 3.12 x64 rồi chạy `./Install-Pronunciation.ps1` từ thư mục dự án. Script cài môi trường riêng và tải khoảng 940 MB trọng số IPA vào `%LOCALAPPDATA%\QuizApp\Pronunciation`. Chạy lại script khi cập nhật từ bộ chấm cũ để cài thư viện đo F0. Lần chấm đầu có thể lâu hơn do nạp mô hình.

| Tiêu chí | Điểm tối đa | Quy tắc |
| --- | ---: | --- |
| Thanh mẫu (声母) | 20 | So phụ âm đầu của từng âm tiết đã đối chiếu với Pinyin mẫu. |
| Vận mẫu (韵母) | 20 | So nguyên âm, âm đệm và âm cuối của từng âm tiết. |
| Thanh điệu (声调) | 25 | Kết hợp đường cao độ F0 với thanh do mô hình IPA nhận dạng. Xét một số biến điệu phổ biến của thanh 3, 一 và 不. Thanh nhẹ dùng thêm thời lượng phát âm tương đối. |
| Độ chính xác âm tiết (音节准确度) | 25 | Đối chiếu đúng thứ tự và trừ điểm âm tiết sai, thiếu hoặc đọc thêm. |
| Độ tự nhiên (自然度) | 10 | Ước tính nhịp đọc qua thời lượng âm tiết và khoảng ngắt, có tính đến dấu câu. |

Điểm tổng là tổng điểm năm tiêu chí. Bộ chấm đối chiếu âm phát ra với Pinyin mẫu và đo thanh điệu từ cả mô hình âm vị lẫn cao độ. Các chữ đồng âm có thể cho cùng điểm phát âm. Khi nhận dạng chữ và âm vị cho kết quả khác nhau, ứng dụng hiện các điểm thành phần xác định được và báo thiếu căn cứ tính tổng. Tiếng rung mạnh hoặc tiếng người nói chồng lên giọng người học có thể khiến bản thu cần được ghi lại.

---

## 🏗 Cấu Trúc Dự Án (Mô Hình MVVM)

```
e:/qz/
├── DatabaseSetup.sql             # Kịch bản SQL tạo QuizDb, Questions, ChineseVocabularies
├── QuizApp.csproj                # Cấu hình project .NET 9 WPF
├── App.xaml / App.xaml.cs        # Khởi chạy ứng dụng & Resource Dictionary
├── MainWindow.xaml / .cs         # Cửa sổ chính & thanh điều hướng đa năng
├── Models/
│   ├── ChineseVocabulary.cs      # Thực thể từ vựng tiếng Trung HSK 1~6
│   ├── HskLevelProgress.cs       # Mô hình tiến độ & mục lục các cấp độ HSK
│   ├── Question.cs               # Thực thể câu hỏi trắc nghiệm
│   ├── QuizResult.cs             # Kết quả đánh giá bài thi
│   └── UserAnswer.cs             # Dữ liệu lựa chọn câu trả lời
├── Services/
│   ├── IHskService.cs            # Interface nghiệp vụ từ vựng & quiz HSK
│   ├── HskService.cs             # Xử lý import Excel, thống kê, sinh quiz
│   ├── IDatabaseService.cs       # Interface kết nối CSDL chung
│   └── SqlServerDatabaseService.cs # Thao tác với Microsoft.Data.SqlClient
├── ViewModels/
│   ├── ViewModelBase.cs          # Lớp cơ sở INotifyPropertyChanged
│   ├── RelayCommand.cs           # Lớp triển khai ICommand
│   ├── MainViewModel.cs          # Quản lý điều hướng toàn bộ ứng dụng
│   ├── DashboardViewModel.cs     # Logic Trang tổng quan lộ trình tiếng Trung
│   ├── VocabListViewModel.cs     # Logic Sổ tay từ vựng & tra cứu HSK
│   ├── TypingPracticeViewModel.cs# Logic Phòng luyện gõ từ mới & câu ví dụ
│   ├── QuizViewModel.cs          # Logic bài thi trắc nghiệm phản xạ
│   ├── OptionViewModel.cs        # Trạng thái từng lựa chọn A-B-C-D
│   ├── ResultViewModel.cs        # Tổng kết điểm số & đánh giá
│   ├── QuestionManagementViewModel.cs # Quản lý ngân hàng câu hỏi
│   └── DatabaseSettingsViewModel.cs   # Quản lý chuỗi kết nối SQL Server
├── Views/
│   ├── DashboardView.xaml        # Giao diện Trang tổng quan (Learning Hub)
│   ├── VocabListView.xaml        # Giao diện Sổ tay từ vựng & tìm kiếm
│   ├── TypingPracticeView.xaml   # Giao diện Phòng luyện gõ HSK
│   ├── QuizView.xaml             # Giao diện làm bài trắc nghiệm (Figma)
│   ├── OptionCardControl.xaml    # UserControl thẻ lựa chọn A-B-C-D
│   ├── ResultView.xaml           # Giao diện tổng kết kết quả
│   ├── QuestionManagementView.xaml # Giao diện ngân hàng câu hỏi
│   └── DatabaseSettingsView.xaml # Giao diện cấu hình kết nối SQL
└── Styles/
    ├── Colors.xaml               # Bảng màu sắc Indigo/Purple cao cấp
    └── Styles.xaml               # Định kiểu nút bấm, thẻ bo góc, đổ bóng
```

---

## 🚀 Hướng Dẫn Chạy Ứng Dụng

Chạy lệnh trong PowerShell tại thư mục `E:\qz`:
```powershell
dotnet run
```
hoặc chạy script có sẵn:
```powershell
.\run.ps1
```
