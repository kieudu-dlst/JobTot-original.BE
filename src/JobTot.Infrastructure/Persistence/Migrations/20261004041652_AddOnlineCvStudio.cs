using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlineCvStudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CvTemplate",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "DesignJson",
                table: "CvTemplate",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PublicationState",
                table: "CvTemplate",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "CvTemplate",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentVersion",
                table: "CandidateCv",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AddColumn<string>(
                name: "OnlineDocumentJson",
                table: "CandidateCv",
                type: "nvarchar(max)",
                nullable: true
            );

            // Starter designs are ordinary editable templates; no fictional ATS scores or usage counts.
            migrationBuilder.InsertData(
                "CvCategory",
                new[] { "Id", "Name", "Status", "CreatedDate" },
                new object[]
                {
                    new Guid("c7000000-0000-4000-8000-000000000001"),
                    "IT & Công nghệ",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                }
            );
            migrationBuilder.InsertData(
                "CvTemplate",
                new[]
                {
                    "Id",
                    "CategoryId",
                    "Name",
                    "Description",
                    "Status",
                    "CreatedDate",
                    "PublicationState",
                    "Version",
                    "DesignJson",
                },
                new object[]
                {
                    new Guid("c8000000-0000-4000-8000-000000000001"),
                    new Guid("c7000000-0000-4000-8000-000000000001"),
                    "Tiên Phong IT 2026",
                    "Tối ưu nội dung cho ngành công nghệ, thể hiện kỹ năng và dự án nổi bật.",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                    "published",
                    new Guid("c8000000-0000-4000-8000-000000000001"),
                    """{"fullName":"NGUYỄN HOÀNG NAM","headline":"Senior Fullstack Architect & Cloud DevOps Lead","email":"email@example.com","phone":"0900 000 000","address":"Hà Nội","website":"linkedin.com/in/ten-cua-ban","layout":"two-column","color":"#ff5722","font":"Roboto","fontSize":10.5,"margin":15,"sections":[{"id":"summary","title":"TÓM TẮT CHUYÊN MÔN","body":"Trình bày kinh nghiệm và thế mạnh phù hợp với vị trí ứng tuyển. Nêu lĩnh vực chuyên môn, giá trị bạn có thể mang lại và định hướng phát triển.","visible":true},{"id":"experience","title":"KINH NGHIỆM LÀM VIỆC","body":"TÊN CÔNG TY • Chức danh                         2023 – Hiện tại\n• Mô tả trách nhiệm và phạm vi công việc của bạn.\n• Trình bày một thành tựu nổi bật bằng số liệu thực tế.\n• Nêu công cụ, phương pháp hoặc kỹ năng sử dụng.\n\nTÊN CÔNG TY TRƯỚC • Chức danh                   2021 – 2023\n• Mô tả công việc liên quan đến vị trí ứng tuyển.\n• Chia sẻ kết quả và bài học chuyên môn.","visible":true},{"id":"skills","title":"KỸ NĂNG CHUYÊN MÔN","body":"CHUYÊN MÔN\nLiệt kê các kỹ năng và công cụ phù hợp với vị trí.\n\nNGOẠI NGỮ & KỸ NĂNG KHÁC\nGiao tiếp, làm việc nhóm, giải quyết vấn đề.","visible":true},{"id":"education","title":"HỌC VẤN & CHỨNG CHỈ","body":"TÊN TRƯỜNG • Chuyên ngành                     2017 – 2021\nBằng cấp, chương trình đào tạo và thành tích liên quan.\n\nCHỨNG CHỈ\nTên chứng chỉ • Đơn vị cấp • Năm cấp","visible":true},{"id":"projects","title":"DỰ ÁN NỔI BẬT","body":"TÊN DỰ ÁN • Vai trò\nMục tiêu, đóng góp của bạn và kết quả đạt được.\nThêm đường dẫn tham khảo nếu phù hợp.","visible":true}]}""",
                }
            );
            migrationBuilder.InsertData(
                "CvCategory",
                new[] { "Id", "Name", "Status", "CreatedDate" },
                new object[]
                {
                    new Guid("c7000000-0000-4000-8000-000000000002"),
                    "Quản lý & Điều hành",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                }
            );
            migrationBuilder.InsertData(
                "CvTemplate",
                new[]
                {
                    "Id",
                    "CategoryId",
                    "Name",
                    "Description",
                    "Status",
                    "CreatedDate",
                    "PublicationState",
                    "Version",
                    "DesignJson",
                },
                new object[]
                {
                    new Guid("c8000000-0000-4000-8000-000000000002"),
                    new Guid("c7000000-0000-4000-8000-000000000002"),
                    "Dẫn Đầu – Quản Trị & Điều Hành",
                    "Bố cục rõ ràng cho kinh nghiệm quản lý, chiến lược và kết quả kinh doanh.",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                    "published",
                    new Guid("c8000000-0000-4000-8000-000000000002"),
                    """{"fullName":"NGUYỄN HOÀNG NAM","headline":"Giám đốc vận hành","email":"email@example.com","phone":"0900 000 000","address":"Hà Nội","website":"linkedin.com/in/ten-cua-ban","layout":"one-column","color":"#243044","font":"Roboto","fontSize":10.5,"margin":15,"sections":[{"id":"summary","title":"TÓM TẮT CHUYÊN MÔN","body":"Trình bày kinh nghiệm và thế mạnh phù hợp với vị trí ứng tuyển. Nêu lĩnh vực chuyên môn, giá trị bạn có thể mang lại và định hướng phát triển.","visible":true},{"id":"experience","title":"KINH NGHIỆM LÀM VIỆC","body":"TÊN CÔNG TY • Chức danh                         2023 – Hiện tại\n• Mô tả trách nhiệm và phạm vi công việc của bạn.\n• Trình bày một thành tựu nổi bật bằng số liệu thực tế.\n• Nêu công cụ, phương pháp hoặc kỹ năng sử dụng.\n\nTÊN CÔNG TY TRƯỚC • Chức danh                   2021 – 2023\n• Mô tả công việc liên quan đến vị trí ứng tuyển.\n• Chia sẻ kết quả và bài học chuyên môn.","visible":true},{"id":"skills","title":"KỸ NĂNG CHUYÊN MÔN","body":"CHUYÊN MÔN\nLiệt kê các kỹ năng và công cụ phù hợp với vị trí.\n\nNGOẠI NGỮ & KỸ NĂNG KHÁC\nGiao tiếp, làm việc nhóm, giải quyết vấn đề.","visible":true},{"id":"education","title":"HỌC VẤN & CHỨNG CHỈ","body":"TÊN TRƯỜNG • Chuyên ngành                     2017 – 2021\nBằng cấp, chương trình đào tạo và thành tích liên quan.\n\nCHỨNG CHỈ\nTên chứng chỉ • Đơn vị cấp • Năm cấp","visible":true},{"id":"projects","title":"DỰ ÁN NỔI BẬT","body":"TÊN DỰ ÁN • Vai trò\nMục tiêu, đóng góp của bạn và kết quả đạt được.\nThêm đường dẫn tham khảo nếu phù hợp.","visible":true}]}""",
                }
            );
            migrationBuilder.InsertData(
                "CvCategory",
                new[] { "Id", "Name", "Status", "CreatedDate" },
                new object[]
                {
                    new Guid("c7000000-0000-4000-8000-000000000003"),
                    "Tối giản",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                }
            );
            migrationBuilder.InsertData(
                "CvTemplate",
                new[]
                {
                    "Id",
                    "CategoryId",
                    "Name",
                    "Description",
                    "Status",
                    "CreatedDate",
                    "PublicationState",
                    "Version",
                    "DesignJson",
                },
                new object[]
                {
                    new Guid("c8000000-0000-4000-8000-000000000003"),
                    new Guid("c7000000-0000-4000-8000-000000000003"),
                    "Tối Giản Tinh Tế",
                    "Một cột gọn gàng, tập trung vào thành tựu và thông tin quan trọng.",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                    "published",
                    new Guid("c8000000-0000-4000-8000-000000000003"),
                    """{"fullName":"NGUYỄN HOÀNG NAM","headline":"Chuyên viên phát triển kinh doanh","email":"email@example.com","phone":"0900 000 000","address":"Hà Nội","website":"linkedin.com/in/ten-cua-ban","layout":"one-column","color":"#243044","font":"Roboto","fontSize":10.5,"margin":15,"sections":[{"id":"summary","title":"TÓM TẮT CHUYÊN MÔN","body":"Trình bày kinh nghiệm và thế mạnh phù hợp với vị trí ứng tuyển. Nêu lĩnh vực chuyên môn, giá trị bạn có thể mang lại và định hướng phát triển.","visible":true},{"id":"experience","title":"KINH NGHIỆM LÀM VIỆC","body":"TÊN CÔNG TY • Chức danh                         2023 – Hiện tại\n• Mô tả trách nhiệm và phạm vi công việc của bạn.\n• Trình bày một thành tựu nổi bật bằng số liệu thực tế.\n• Nêu công cụ, phương pháp hoặc kỹ năng sử dụng.\n\nTÊN CÔNG TY TRƯỚC • Chức danh                   2021 – 2023\n• Mô tả công việc liên quan đến vị trí ứng tuyển.\n• Chia sẻ kết quả và bài học chuyên môn.","visible":true},{"id":"skills","title":"KỸ NĂNG CHUYÊN MÔN","body":"CHUYÊN MÔN\nLiệt kê các kỹ năng và công cụ phù hợp với vị trí.\n\nNGOẠI NGỮ & KỸ NĂNG KHÁC\nGiao tiếp, làm việc nhóm, giải quyết vấn đề.","visible":true},{"id":"education","title":"HỌC VẤN & CHỨNG CHỈ","body":"TÊN TRƯỜNG • Chuyên ngành                     2017 – 2021\nBằng cấp, chương trình đào tạo và thành tích liên quan.\n\nCHỨNG CHỈ\nTên chứng chỉ • Đơn vị cấp • Năm cấp","visible":true},{"id":"projects","title":"DỰ ÁN NỔI BẬT","body":"TÊN DỰ ÁN • Vai trò\nMục tiêu, đóng góp của bạn và kết quả đạt được.\nThêm đường dẫn tham khảo nếu phù hợp.","visible":true}]}""",
                }
            );
            migrationBuilder.InsertData(
                "CvCategory",
                new[] { "Id", "Name", "Status", "CreatedDate" },
                new object[]
                {
                    new Guid("c7000000-0000-4000-8000-000000000004"),
                    "Design & Marketing",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                }
            );
            migrationBuilder.InsertData(
                "CvTemplate",
                new[]
                {
                    "Id",
                    "CategoryId",
                    "Name",
                    "Description",
                    "Status",
                    "CreatedDate",
                    "PublicationState",
                    "Version",
                    "DesignJson",
                },
                new object[]
                {
                    new Guid("c8000000-0000-4000-8000-000000000004"),
                    new Guid("c7000000-0000-4000-8000-000000000004"),
                    "Sáng Tạo Đa Chiều",
                    "Màu sắc nổi bật, phù hợp thể hiện dự án và danh mục sáng tạo.",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                    "published",
                    new Guid("c8000000-0000-4000-8000-000000000004"),
                    """{"fullName":"NGUYỄN HOÀNG NAM","headline":"Chuyên viên Marketing","email":"email@example.com","phone":"0900 000 000","address":"Hà Nội","website":"linkedin.com/in/ten-cua-ban","layout":"sidebar","color":"#8a42ed","font":"Roboto","fontSize":10.5,"margin":15,"sections":[{"id":"summary","title":"TÓM TẮT CHUYÊN MÔN","body":"Trình bày kinh nghiệm và thế mạnh phù hợp với vị trí ứng tuyển. Nêu lĩnh vực chuyên môn, giá trị bạn có thể mang lại và định hướng phát triển.","visible":true},{"id":"experience","title":"KINH NGHIỆM LÀM VIỆC","body":"TÊN CÔNG TY • Chức danh                         2023 – Hiện tại\n• Mô tả trách nhiệm và phạm vi công việc của bạn.\n• Trình bày một thành tựu nổi bật bằng số liệu thực tế.\n• Nêu công cụ, phương pháp hoặc kỹ năng sử dụng.\n\nTÊN CÔNG TY TRƯỚC • Chức danh                   2021 – 2023\n• Mô tả công việc liên quan đến vị trí ứng tuyển.\n• Chia sẻ kết quả và bài học chuyên môn.","visible":true},{"id":"skills","title":"KỸ NĂNG CHUYÊN MÔN","body":"CHUYÊN MÔN\nLiệt kê các kỹ năng và công cụ phù hợp với vị trí.\n\nNGOẠI NGỮ & KỸ NĂNG KHÁC\nGiao tiếp, làm việc nhóm, giải quyết vấn đề.","visible":true},{"id":"education","title":"HỌC VẤN & CHỨNG CHỈ","body":"TÊN TRƯỜNG • Chuyên ngành                     2017 – 2021\nBằng cấp, chương trình đào tạo và thành tích liên quan.\n\nCHỨNG CHỈ\nTên chứng chỉ • Đơn vị cấp • Năm cấp","visible":true},{"id":"projects","title":"DỰ ÁN NỔI BẬT","body":"TÊN DỰ ÁN • Vai trò\nMục tiêu, đóng góp của bạn và kết quả đạt được.\nThêm đường dẫn tham khảo nếu phù hợp.","visible":true}]}""",
                }
            );
            migrationBuilder.InsertData(
                "CvCategory",
                new[] { "Id", "Name", "Status", "CreatedDate" },
                new object[]
                {
                    new Guid("c7000000-0000-4000-8000-000000000005"),
                    "Tài chính & Ngân hàng",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                }
            );
            migrationBuilder.InsertData(
                "CvTemplate",
                new[]
                {
                    "Id",
                    "CategoryId",
                    "Name",
                    "Description",
                    "Status",
                    "CreatedDate",
                    "PublicationState",
                    "Version",
                    "DesignJson",
                },
                new object[]
                {
                    new Guid("c8000000-0000-4000-8000-000000000005"),
                    new Guid("c7000000-0000-4000-8000-000000000005"),
                    "Chuyên Nghiệp Tài Chính",
                    "Cấu trúc mạch lạc cho nghiệp vụ tài chính, phân tích và chứng chỉ.",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                    "published",
                    new Guid("c8000000-0000-4000-8000-000000000005"),
                    """{"fullName":"NGUYỄN HOÀNG NAM","headline":"Chuyên viên tài chính","email":"email@example.com","phone":"0900 000 000","address":"Hà Nội","website":"linkedin.com/in/ten-cua-ban","layout":"two-column","color":"#2189bb","font":"Roboto","fontSize":10.5,"margin":15,"sections":[{"id":"summary","title":"TÓM TẮT CHUYÊN MÔN","body":"Trình bày kinh nghiệm và thế mạnh phù hợp với vị trí ứng tuyển. Nêu lĩnh vực chuyên môn, giá trị bạn có thể mang lại và định hướng phát triển.","visible":true},{"id":"experience","title":"KINH NGHIỆM LÀM VIỆC","body":"TÊN CÔNG TY • Chức danh                         2023 – Hiện tại\n• Mô tả trách nhiệm và phạm vi công việc của bạn.\n• Trình bày một thành tựu nổi bật bằng số liệu thực tế.\n• Nêu công cụ, phương pháp hoặc kỹ năng sử dụng.\n\nTÊN CÔNG TY TRƯỚC • Chức danh                   2021 – 2023\n• Mô tả công việc liên quan đến vị trí ứng tuyển.\n• Chia sẻ kết quả và bài học chuyên môn.","visible":true},{"id":"skills","title":"KỸ NĂNG CHUYÊN MÔN","body":"CHUYÊN MÔN\nLiệt kê các kỹ năng và công cụ phù hợp với vị trí.\n\nNGOẠI NGỮ & KỸ NĂNG KHÁC\nGiao tiếp, làm việc nhóm, giải quyết vấn đề.","visible":true},{"id":"education","title":"HỌC VẤN & CHỨNG CHỈ","body":"TÊN TRƯỜNG • Chuyên ngành                     2017 – 2021\nBằng cấp, chương trình đào tạo và thành tích liên quan.\n\nCHỨNG CHỈ\nTên chứng chỉ • Đơn vị cấp • Năm cấp","visible":true},{"id":"projects","title":"DỰ ÁN NỔI BẬT","body":"TÊN DỰ ÁN • Vai trò\nMục tiêu, đóng góp của bạn và kết quả đạt được.\nThêm đường dẫn tham khảo nếu phù hợp.","visible":true}]}""",
                }
            );
            migrationBuilder.InsertData(
                "CvCategory",
                new[] { "Id", "Name", "Status", "CreatedDate" },
                new object[]
                {
                    new Guid("c7000000-0000-4000-8000-000000000006"),
                    "Fresher & Sinh viên",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                }
            );
            migrationBuilder.InsertData(
                "CvTemplate",
                new[]
                {
                    "Id",
                    "CategoryId",
                    "Name",
                    "Description",
                    "Status",
                    "CreatedDate",
                    "PublicationState",
                    "Version",
                    "DesignJson",
                },
                new object[]
                {
                    new Guid("c8000000-0000-4000-8000-000000000006"),
                    new Guid("c7000000-0000-4000-8000-000000000006"),
                    "Khởi Đầu Vững Chắc",
                    "Nhấn mạnh học vấn, hoạt động và dự án dành cho ứng viên mới.",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                    "published",
                    new Guid("c8000000-0000-4000-8000-000000000006"),
                    """{"fullName":"NGUYỄN HOÀNG NAM","headline":"Thực tập sinh","email":"email@example.com","phone":"0900 000 000","address":"Hà Nội","website":"linkedin.com/in/ten-cua-ban","layout":"one-column","color":"#00a783","font":"Roboto","fontSize":10.5,"margin":15,"sections":[{"id":"summary","title":"TÓM TẮT CHUYÊN MÔN","body":"Trình bày kinh nghiệm và thế mạnh phù hợp với vị trí ứng tuyển. Nêu lĩnh vực chuyên môn, giá trị bạn có thể mang lại và định hướng phát triển.","visible":true},{"id":"experience","title":"KINH NGHIỆM LÀM VIỆC","body":"TÊN CÔNG TY • Chức danh                         2023 – Hiện tại\n• Mô tả trách nhiệm và phạm vi công việc của bạn.\n• Trình bày một thành tựu nổi bật bằng số liệu thực tế.\n• Nêu công cụ, phương pháp hoặc kỹ năng sử dụng.\n\nTÊN CÔNG TY TRƯỚC • Chức danh                   2021 – 2023\n• Mô tả công việc liên quan đến vị trí ứng tuyển.\n• Chia sẻ kết quả và bài học chuyên môn.","visible":true},{"id":"skills","title":"KỸ NĂNG CHUYÊN MÔN","body":"CHUYÊN MÔN\nLiệt kê các kỹ năng và công cụ phù hợp với vị trí.\n\nNGOẠI NGỮ & KỸ NĂNG KHÁC\nGiao tiếp, làm việc nhóm, giải quyết vấn đề.","visible":true},{"id":"education","title":"HỌC VẤN & CHỨNG CHỈ","body":"TÊN TRƯỜNG • Chuyên ngành                     2017 – 2021\nBằng cấp, chương trình đào tạo và thành tích liên quan.\n\nCHỨNG CHỈ\nTên chứng chỉ • Đơn vị cấp • Năm cấp","visible":true},{"id":"projects","title":"DỰ ÁN NỔI BẬT","body":"TÊN DỰ ÁN • Vai trò\nMục tiêu, đóng góp của bạn và kết quả đạt được.\nThêm đường dẫn tham khảo nếu phù hợp.","visible":true}]}""",
                }
            );
            migrationBuilder.InsertData(
                "CvCategory",
                new[] { "Id", "Name", "Status", "CreatedDate" },
                new object[]
                {
                    new Guid("c7000000-0000-4000-8000-000000000007"),
                    "Global & Ngoại ngữ",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                }
            );
            migrationBuilder.InsertData(
                "CvTemplate",
                new[]
                {
                    "Id",
                    "CategoryId",
                    "Name",
                    "Description",
                    "Status",
                    "CreatedDate",
                    "PublicationState",
                    "Version",
                    "DesignJson",
                },
                new object[]
                {
                    new Guid("c8000000-0000-4000-8000-000000000007"),
                    new Guid("c7000000-0000-4000-8000-000000000007"),
                    "Dấu Ấn Toàn Cầu",
                    "Thiết kế đơn giản để trình bày kinh nghiệm và ngoại ngữ.",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                    "published",
                    new Guid("c8000000-0000-4000-8000-000000000007"),
                    """{"fullName":"NGUYỄN HOÀNG NAM","headline":"International Business Specialist","email":"email@example.com","phone":"0900 000 000","address":"Hà Nội","website":"linkedin.com/in/ten-cua-ban","layout":"one-column","color":"#2189bb","font":"Roboto","fontSize":10.5,"margin":15,"sections":[{"id":"summary","title":"TÓM TẮT CHUYÊN MÔN","body":"Trình bày kinh nghiệm và thế mạnh phù hợp với vị trí ứng tuyển. Nêu lĩnh vực chuyên môn, giá trị bạn có thể mang lại và định hướng phát triển.","visible":true},{"id":"experience","title":"KINH NGHIỆM LÀM VIỆC","body":"TÊN CÔNG TY • Chức danh                         2023 – Hiện tại\n• Mô tả trách nhiệm và phạm vi công việc của bạn.\n• Trình bày một thành tựu nổi bật bằng số liệu thực tế.\n• Nêu công cụ, phương pháp hoặc kỹ năng sử dụng.\n\nTÊN CÔNG TY TRƯỚC • Chức danh                   2021 – 2023\n• Mô tả công việc liên quan đến vị trí ứng tuyển.\n• Chia sẻ kết quả và bài học chuyên môn.","visible":true},{"id":"skills","title":"KỸ NĂNG CHUYÊN MÔN","body":"CHUYÊN MÔN\nLiệt kê các kỹ năng và công cụ phù hợp với vị trí.\n\nNGOẠI NGỮ & KỸ NĂNG KHÁC\nGiao tiếp, làm việc nhóm, giải quyết vấn đề.","visible":true},{"id":"education","title":"HỌC VẤN & CHỨNG CHỈ","body":"TÊN TRƯỜNG • Chuyên ngành                     2017 – 2021\nBằng cấp, chương trình đào tạo và thành tích liên quan.\n\nCHỨNG CHỈ\nTên chứng chỉ • Đơn vị cấp • Năm cấp","visible":true},{"id":"projects","title":"DỰ ÁN NỔI BẬT","body":"TÊN DỰ ÁN • Vai trò\nMục tiêu, đóng góp của bạn và kết quả đạt được.\nThêm đường dẫn tham khảo nếu phù hợp.","visible":true}]}""",
                }
            );
            migrationBuilder.InsertData(
                "CvCategory",
                new[] { "Id", "Name", "Status", "CreatedDate" },
                new object[]
                {
                    new Guid("c7000000-0000-4000-8000-000000000008"),
                    "Y tế & Dược phẩm",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                }
            );
            migrationBuilder.InsertData(
                "CvTemplate",
                new[]
                {
                    "Id",
                    "CategoryId",
                    "Name",
                    "Description",
                    "Status",
                    "CreatedDate",
                    "PublicationState",
                    "Version",
                    "DesignJson",
                },
                new object[]
                {
                    new Guid("c8000000-0000-4000-8000-000000000008"),
                    new Guid("c7000000-0000-4000-8000-000000000008"),
                    "Thanh Lịch Y Dược",
                    "Bố cục theo khối dành cho chuyên môn, chứng chỉ và kinh nghiệm thực hành.",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                    "published",
                    new Guid("c8000000-0000-4000-8000-000000000008"),
                    """{"fullName":"NGUYỄN HOÀNG NAM","headline":"Chuyên viên y tế","email":"email@example.com","phone":"0900 000 000","address":"Hà Nội","website":"linkedin.com/in/ten-cua-ban","layout":"bento","color":"#00a783","font":"Roboto","fontSize":10.5,"margin":15,"sections":[{"id":"summary","title":"TÓM TẮT CHUYÊN MÔN","body":"Trình bày kinh nghiệm và thế mạnh phù hợp với vị trí ứng tuyển. Nêu lĩnh vực chuyên môn, giá trị bạn có thể mang lại và định hướng phát triển.","visible":true},{"id":"experience","title":"KINH NGHIỆM LÀM VIỆC","body":"TÊN CÔNG TY • Chức danh                         2023 – Hiện tại\n• Mô tả trách nhiệm và phạm vi công việc của bạn.\n• Trình bày một thành tựu nổi bật bằng số liệu thực tế.\n• Nêu công cụ, phương pháp hoặc kỹ năng sử dụng.\n\nTÊN CÔNG TY TRƯỚC • Chức danh                   2021 – 2023\n• Mô tả công việc liên quan đến vị trí ứng tuyển.\n• Chia sẻ kết quả và bài học chuyên môn.","visible":true},{"id":"skills","title":"KỸ NĂNG CHUYÊN MÔN","body":"CHUYÊN MÔN\nLiệt kê các kỹ năng và công cụ phù hợp với vị trí.\n\nNGOẠI NGỮ & KỸ NĂNG KHÁC\nGiao tiếp, làm việc nhóm, giải quyết vấn đề.","visible":true},{"id":"education","title":"HỌC VẤN & CHỨNG CHỈ","body":"TÊN TRƯỜNG • Chuyên ngành                     2017 – 2021\nBằng cấp, chương trình đào tạo và thành tích liên quan.\n\nCHỨNG CHỈ\nTên chứng chỉ • Đơn vị cấp • Năm cấp","visible":true},{"id":"projects","title":"DỰ ÁN NỔI BẬT","body":"TÊN DỰ ÁN • Vai trò\nMục tiêu, đóng góp của bạn và kết quả đạt được.\nThêm đường dẫn tham khảo nếu phù hợp.","visible":true}]}""",
                }
            );
            migrationBuilder.InsertData(
                "CvCategory",
                new[] { "Id", "Name", "Status", "CreatedDate" },
                new object[]
                {
                    new Guid("c7000000-0000-4000-8000-000000000009"),
                    "Sales & Kinh doanh",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                }
            );
            migrationBuilder.InsertData(
                "CvTemplate",
                new[]
                {
                    "Id",
                    "CategoryId",
                    "Name",
                    "Description",
                    "Status",
                    "CreatedDate",
                    "PublicationState",
                    "Version",
                    "DesignJson",
                },
                new object[]
                {
                    new Guid("c8000000-0000-4000-8000-000000000009"),
                    new Guid("c7000000-0000-4000-8000-000000000009"),
                    "Đột Phá Bán Hàng B2B",
                    "Tập trung vào doanh số, khách hàng và thành tích kinh doanh.",
                    1,
                    new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                    "published",
                    new Guid("c8000000-0000-4000-8000-000000000009"),
                    """{"fullName":"NGUYỄN HOÀNG NAM","headline":"Chuyên viên kinh doanh B2B","email":"email@example.com","phone":"0900 000 000","address":"Hà Nội","website":"linkedin.com/in/ten-cua-ban","layout":"two-column","color":"#ff5722","font":"Roboto","fontSize":10.5,"margin":15,"sections":[{"id":"summary","title":"TÓM TẮT CHUYÊN MÔN","body":"Trình bày kinh nghiệm và thế mạnh phù hợp với vị trí ứng tuyển. Nêu lĩnh vực chuyên môn, giá trị bạn có thể mang lại và định hướng phát triển.","visible":true},{"id":"experience","title":"KINH NGHIỆM LÀM VIỆC","body":"TÊN CÔNG TY • Chức danh                         2023 – Hiện tại\n• Mô tả trách nhiệm và phạm vi công việc của bạn.\n• Trình bày một thành tựu nổi bật bằng số liệu thực tế.\n• Nêu công cụ, phương pháp hoặc kỹ năng sử dụng.\n\nTÊN CÔNG TY TRƯỚC • Chức danh                   2021 – 2023\n• Mô tả công việc liên quan đến vị trí ứng tuyển.\n• Chia sẻ kết quả và bài học chuyên môn.","visible":true},{"id":"skills","title":"KỸ NĂNG CHUYÊN MÔN","body":"CHUYÊN MÔN\nLiệt kê các kỹ năng và công cụ phù hợp với vị trí.\n\nNGOẠI NGỮ & KỸ NĂNG KHÁC\nGiao tiếp, làm việc nhóm, giải quyết vấn đề.","visible":true},{"id":"education","title":"HỌC VẤN & CHỨNG CHỈ","body":"TÊN TRƯỜNG • Chuyên ngành                     2017 – 2021\nBằng cấp, chương trình đào tạo và thành tích liên quan.\n\nCHỨNG CHỈ\nTên chứng chỉ • Đơn vị cấp • Năm cấp","visible":true},{"id":"projects","title":"DỰ ÁN NỔI BẬT","body":"TÊN DỰ ÁN • Vai trò\nMục tiêu, đóng góp của bạn và kết quả đạt được.\nThêm đường dẫn tham khảo nếu phù hợp.","visible":true}]}""",
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM CvTemplate WHERE Id IN ('c8000000-0000-4000-8000-000000000001','c8000000-0000-4000-8000-000000000002','c8000000-0000-4000-8000-000000000003','c8000000-0000-4000-8000-000000000004','c8000000-0000-4000-8000-000000000005','c8000000-0000-4000-8000-000000000006','c8000000-0000-4000-8000-000000000007','c8000000-0000-4000-8000-000000000008','c8000000-0000-4000-8000-000000000009')"
            );
            migrationBuilder.Sql(
                "DELETE FROM CvCategory WHERE Id IN ('c7000000-0000-4000-8000-000000000001','c7000000-0000-4000-8000-000000000002','c7000000-0000-4000-8000-000000000003','c7000000-0000-4000-8000-000000000004','c7000000-0000-4000-8000-000000000005','c7000000-0000-4000-8000-000000000006','c7000000-0000-4000-8000-000000000007','c7000000-0000-4000-8000-000000000008','c7000000-0000-4000-8000-000000000009')"
            );
            migrationBuilder.DropColumn(name: "Description", table: "CvTemplate");

            migrationBuilder.DropColumn(name: "DesignJson", table: "CvTemplate");

            migrationBuilder.DropColumn(name: "PublicationState", table: "CvTemplate");

            migrationBuilder.DropColumn(name: "Version", table: "CvTemplate");

            migrationBuilder.DropColumn(name: "DocumentVersion", table: "CandidateCv");

            migrationBuilder.DropColumn(name: "OnlineDocumentJson", table: "CandidateCv");
        }
    }
}
