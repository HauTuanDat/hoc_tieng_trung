# Bối cảnh dự án Qz

Tài liệu này giúp AI hiểu Qz đang làm gì, công nghệ đang dùng và nơi bắt đầu đọc mã. Quy tắc phân công, worktree và bàn giao nằm trong [AGENTS.md](AGENTS.md).

## Ứng dụng hiện tại

Qz, hiển thị tên **Mộc Ngữ** trên giao diện, là ứng dụng desktop Windows để học tiếng Trung bằng tiếng Việt. Nội dung được tổ chức chủ yếu theo HSK và có thêm một số nhóm từ chuyên ngành. Các luồng trong mã hiện tại gồm:

- Học và tra cứu từ vựng, xem chữ Hán, Pinyin, nghĩa và câu mẫu. Theo dõi từ đã học và tiến độ theo cấp.
- Luyện trắc nghiệm theo đợt, ôn câu sai, luyện gõ từ và câu, sắp xếp câu.
- Luyện nói theo từ hoặc câu mẫu. Người dùng nghe mẫu, thu qua mic và nhận kết quả nhận dạng cùng điểm phát âm.
- Đăng nhập, hồ sơ và cấu hình cơ sở dữ liệu trong ứng dụng.

## Công nghệ đang dùng

| Phần | Công nghệ | Vai trò |
| --- | --- | --- |
| Ứng dụng | C#, .NET 9 cho Windows, WPF/XAML, MVVM | Giao diện, điều hướng, trạng thái màn hình và các lệnh của người dùng |
| Dữ liệu học | Microsoft SQL Server, `Microsoft.Data.SqlClient` | Lưu từ vựng, người dùng và tiến độ |
| Nhập từ vựng | `ExcelDataReader`, `SqlBulkCopy` | Đọc dữ liệu từ Excel và nạp vào SQL Server |
| Thu âm | NAudio, Windows WASAPI, WAV PCM mono 16 kHz | Thu từ micro mặc định, hiển thị mức tín hiệu và chuẩn bị bản nghe lại |
| Nhận dạng chữ | FunASR Paraformer ONNX, sherpa-onnx, Zipformer khi cần đối chiếu | Chuyển giọng nói thành chữ Hán bằng mô hình chạy trên máy |
| Chấm phát âm | Python 3.12 x64, Mandarin IPA ASR, Pypinyin, Praat qua Parselmouth | Đối chiếu âm vị với Pinyin mẫu và đo cao độ F0 cho thanh điệu |
| Âm mẫu và tra cứu | Dịch vụ phát âm tiếng Trung và dịch vụ từ điển | Phát nội dung mẫu và tra nghĩa từ qua các dịch vụ hiện có |

## Luồng luyện nói cần nắm

1. Màn hình lấy chữ Hán và Pinyin mẫu của từ hoặc câu đang học.
2. Qz thu âm, giảm tiếng ù và tiếng nền, rồi chuẩn bị bản nhận dạng và bản nghe lại.
3. FunASR trả về chữ Hán. Zipformer cung cấp phương án đối chiếu trong một số trường hợp.
4. Bộ chấm dùng âm vị, Pinyin mẫu và cao độ để đánh giá **thanh mẫu 20**, **vận mẫu 20**, **thanh điệu 25**, **độ chính xác âm tiết 25** và **độ tự nhiên 10**. Khi thiếu dữ liệu cho một tiêu chí, giao diện có thể hiện điểm một phần.

## Bắt đầu đọc mã

| Muốn làm việc với | Đọc trước |
| --- | --- |
| Cấu hình .NET và thư viện | [QuizApp.csproj](QuizApp.csproj) |
| Điều hướng và màn hình | [MainViewModel.cs](ViewModels/MainViewModel.cs), `Views/`, `ViewModels/`, `Styles/` |
| Từ vựng, tiến độ và trắc nghiệm | [IHskService.cs](Services/IHskService.cs), [HskService.cs](Services/HskService.cs) |
| Từ điển | [IDictionaryApiService.cs](Services/IDictionaryApiService.cs), [DictionaryApiService.cs](Services/DictionaryApiService.cs) |
| Giao diện và điều phối luyện nói | [SpeakingPracticeView.xaml](Views/SpeakingPracticeView.xaml), [SpeakingPracticeViewModel.cs](ViewModels/SpeakingPracticeViewModel.cs) |
| Thu âm, nhận dạng và chấm | [AudioRecorderService.cs](Services/AudioRecorderService.cs), [FunAsrTranscriptionService.cs](Services/FunAsrTranscriptionService.cs), [LocalPronunciationService.cs](Services/LocalPronunciationService.cs), `Pronunciation/` |
| Cơ sở dữ liệu | [DatabaseSetup.sql](DatabaseSetup.sql), [SqlServerDatabaseService.cs](Services/SqlServerDatabaseService.cs) |

## Chạy dự án

Từ thư mục worktree được giao, dùng `dotnet build .\QuizApp.csproj` để build hoặc `dotnet run --project .\QuizApp.csproj` để chạy. Cấu hình SQL Server cho luồng cần dữ liệu. Với bộ chấm phát âm trên máy mới, xem phần Luyện nói trong [README.md](README.md) rồi chạy [Install-Pronunciation.ps1](Install-Pronunciation.ps1) để cài môi trường và mô hình.

## Định hướng đang trao đổi

Chatbot trợ lý học tiếng Trung là một hướng đang được thảo luận. Hai chức năng được đề xuất là hỏi về bài đang học và giải thích kết quả phát âm dựa trên điểm do Qz trả về. Việc chọn mô hình, cách chạy chatbot và phạm vi bản đầu sẽ được ghi sau khi chủ dự án xác nhận.

Đối tượng ưu tiên và mục tiêu của bản tiếp theo cũng chờ chủ dự án xác nhận. Các ghi chú ý tưởng khác nằm trong `các tính năng cần phát triển.txt`.
