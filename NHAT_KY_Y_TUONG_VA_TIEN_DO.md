# Tiến độ, quyết định nghiệp vụ và bàn giao

Cập nhật: **09/10/2026 — Người 1, giữ UI master; hoàn thiện giỏ món, đối chiếu cọc, giữ chỗ 1 tiếng/tự hủy và phục vụ tối đa 3 tiếng; đồng bộ bàn giao theo phân công mới**.
File mô tả trạng thái của nhánh đang đọc; không mặc nhiên có nghĩa đã merge vào `master`.
Nhánh bàn giao: `member1-week8-update`, dựng từ bản `master` sạch tại `f7695e2`, đã fetch đối chiếu GitHub ngày 09/10 trước khi bàn giao; master chưa có commit mới hơn. Bản sửa dở `member1-week8` và mốc `member1-week8-refresh` được giữ riêng để đối chiếu. Giữ giao diện và các module của nhóm; nhánh này dùng để review, chỉ nhóm trưởng/người được phân công thực hiện merge sau khi kiểm tra. Mã commit/trạng thái xuất bản đối chiếu trực tiếp trên GitHub, không suy từ ghi chú lịch sử.

## Cách cập nhật file này

- Thành viên hoặc AI hỗ trợ phải kiểm tra nhánh, commit và code hiện tại trước khi làm. Fetch để đối chiếu bản mới; không pull/đổi nhánh đè lên thay đổi chưa lưu.
- Làm xong thì **sửa đúng mục hiện trạng/bàn giao bên dưới**, cập nhật ngày và kết quả kiểm tra. Không nối thêm hàng loạt mục lặp lại chuyện sửa lỗi cũ.
- Phân biệt `Đã làm`, `Cần tiếp tục` và `Ý tưởng chưa chốt`. Nếu quyết định thay đổi, thay nội dung cũ và ghi ngắn lý do/ngày; không để hai hướng trái nhau cùng được coi là đúng.
- Khi nhận việc, đọc **mục 3 (quyền), 5 (phân công), 6 (điểm tích hợp)** rồi đối chiếu controller/service/model thật. AI của từng thành viên cũng phải sửa mục tương ứng sau khi hoàn thành; không nối nhật ký lỗi hoặc ghi thêm một bản bàn giao trái với bản này. Báo đã làm phần nào, kiểm chứng bằng gì và phần nào còn cần người sau.
- Code trên nhánh cập nhật chưa tự có trong `master`. Khi kiểm tra/tích hợp, phải lấy đúng nhánh hoặc commit đã bàn giao; không suy rằng chỉ pull `master` là đã có tất cả. Đọc tài liệu cùng phiên bản code, giữ thay đổi local trước khi chuyển nhánh; không merge nguyên UI nhánh auth cũ vào UI mới của nhóm.
- Không đánh dấu hoàn thành chỉ vì đã có bảng CSDL hoặc nút giao diện. Không ghi mật khẩu thật; thay đổi công thức/combo/CSDL liên quan phải thống nhất với người làm phần đó trước.

## 1. Nền tảng và giao diện giữ từ master

- Giữ trang chủ nhà hàng, danh sách/chi tiết món, ảnh minh họa, đăng nhập/đăng ký khách, đăng nhập nội bộ và CSS chính của nhóm. Không lấy UI nhánh auth cũ thay giao diện mới.
- `/` là trang công khai cho khách xem nhà hàng/thực đơn. Khách đăng nhập tại `/Account/Login`; nhân viên và Admin tại `/admin`.
- Nhân viên/Admin vẫn xem trang nhà hàng để kiểm tra nội dung nhưng **không thấy nút/link Đặt bàn của khách**, kể cả menu, banner và chi tiết món. Khách và người chưa đăng nhập vẫn thấy; truy cập thẳng `/DatBan` vẫn bị kiểm tra role ở server. Nhân viên nhận yêu cầu cho khách tại khu tiếp tân, không dùng form khách.
- Form khách chỉ nhận tài khoản khách. Nhập đúng tài khoản nhân viên ở form khách vẫn báo sai thông tin chung, không tiết lộ vai trò. Email đăng ký không được trùng tài khoản đã tồn tại, kể cả nhân viên.
- `/Account` là **Thông tin cá nhân** cho mọi vai trò, không phải trang chủ. Các trang tài khoản dùng bố cục/style sẵn có của khu khách hoặc khu quản trị, không dùng khung UI auth cũ.
- `/admin/TrangChu` là trang chủ nội bộ với lối tắt theo quyền; Admin vào đây sau đăng nhập, nhân viên vẫn vào chức năng công việc chính. `/` vẫn là web nhà hàng công khai, có liên kết riêng **Xem trang nhà hàng**.
- Menu nội bộ: **Trang chủ → Tài khoản → Bàn & Đặt bàn → Thực đơn → Vận hành → Đánh giá khách hàng**. Nhóm chỉ hiện mục được phép; nhóm một mục hiện trực tiếp, nhóm đang xem tự mở, Đăng xuất ở cuối. Menu và lối tắt dùng chung danh sách để tránh lệch tên/quyền. Không thêm chức năng chưa có chỉ để lấp menu.
- Khung nhỏ/điện thoại: menu nội bộ mặc định thu gọn bằng nút **Menu**, không đẩy nội dung chức năng ra ngoài màn hình. Khi mở, menu cuộn riêng; chuyển trang thì trở về trạng thái thu gọn. Desktop giữ sidebar theo nhóm.
- Hai trang đăng nhập tách luồng sử dụng, **không thay thế bảo mật phía server**. Giấu đường dẫn không bảo vệ được tài khoản đã lộ mật khẩu.

## 2. Thành viên 1 đã bổ sung trên nhánh cập nhật

### Luồng tuần 8 — hiện trạng và quyết định ngày 09/10

- **Chọn món trước rồi đặt bàn:** dùng thực đơn có ảnh trên trang khách hiện có. Bấm chọn món mở khung size, số lượng, ghi chú; giỏ hiển thị các món đã chọn và tổng tạm tính. Bấm đặt bàn để nhập liên hệ, ngày/giờ, số khách, khu vực. Có thể chỉ đặt bàn mà chưa chọn món.
- **Đặt bàn rồi bổ sung món:** từ lịch sử/chi tiết lịch, chọn bổ sung để quay lại đúng thực đơn và giỏ của lịch đó. Server kiểm tra khách sở hữu lịch, lịch còn nhận bổ sung, món/size còn bán. Không dùng điều kiện “đặt trước 30 phút” của tạo lịch mới để cấm thêm món vào lịch có sẵn: lịch 12h vẫn có thể thêm lúc 11h59 nếu chưa kết thúc và món còn phục vụ. Bếp/nhà hàng quyết định thời gian chuẩn bị, không hứa món bổ sung sẽ ra ngay 12h.
- **Giỏ và giá:** server lấy giá hợp lệ khi lưu, lưu tên món/size/giá tại thời điểm khách chốt. Giá thực đơn thay đổi sau đó không sửa giá đã thỏa thuận. Cùng size nhưng ghi chú khác nhau có thể thành hai dòng. Giỏ nháp tách theo tài khoản và lịch; không lưu mật khẩu, thông tin liên hệ hoặc tin giá từ trình duyệt. Mã gửi chống tạo trùng; gửi lại cùng mã với nội dung khác bị chặn. Thay đổi đồng thời cần tải lại để đối chiếu, giữ nháp để khách kiểm tra.
- **AJAX:** tạo lịch, lưu món, ghi nhận cọc và các form nhập liệu được bật phù hợp gửi ngay trong trang. Lỗi nhập liệu, mạng hoặc phiên đăng nhập không xóa dữ liệu đang điền. Cookie vẫn quản lý đăng nhập; AJAX là cách gửi/nhận và hiển thị phản hồi, không thay thế xác thực/CSRF/quyền phía server.
- **Chọn trước khác với gửi bếp:** `MonDatTruoc` lưu món khách chọn, chưa tạo món chờ chế biến ngay. Tiếp tân nhận khách thì chuyển các dòng chưa gửi thành dòng gọi món trong cùng giao dịch. Nếu Admin không có hồ sơ nhân viên, cần chọn nhân viên phụ trách khi gửi bếp; không lấy mã tài khoản giả làm mã nhân viên. Mỗi dòng đặt trước liên kết tối đa một dòng hóa đơn, chống gửi hai lần. FIFO tính lúc thực sự gửi bếp, không lấy thời điểm khách thêm vào giỏ nhiều giờ trước đó.
- **Chuẩn bị sớm:** khách có thể yêu cầu, nhưng nhân viên phải xác nhận và đã nhận đủ cọc thỏa thuận trước khi gửi bếp trước lúc nhận bàn. Món đã gửi bếp hiển thị trạng thái và không cho khách sửa/xóa tự do; bổ sung mới tạo lượt gửi tiếp theo. Người dùng vẫn gọi thêm món trong thời gian phục vụ; không biến món thêm sau nhận bàn thành yêu cầu cọc mới.
- **Cọc tự tính — đã làm 09/10:** lịch online mới dùng `ReservationDepositPolicy`: có món đặt trước thì cọc 50% tổng giá món/size/combo từ DB (làm tròn lên đến 1 đồng); chưa chọn món thì 299.000đ cho cả lịch, không tính theo số khách/bàn. Tiền món 2.000.000đ → cọc 1.000.000đ. Giỏ chỉ hiển thị dự kiến; server tính lại trước khi lưu, không nhận số cọc/giá do khách gửi. Tiếp tân không nhập/đổi số cọc tùy ý cho lịch tự tính. Lịch và giao dịch cũ giữ số tiền, mã, liên kết và thỏa thuận cũ; lịch cũ chưa báo giá có nút Tính cọc và tiếp tục thanh toán.
- **Giữ chỗ và mã ngắn:** kiểm tra sức chứa/khu vực/lịch trùng rồi giữ một hoặc nhiều bàn cùng khu vực trong transaction, sau đó mới đưa khách tới `/DatBan/ThanhToanCoc/{id}`. Chưa xác nhận tiền thì lịch vẫn `ChoCoc`. Hạn mới **1 tiếng (60 phút)**, thay mức 20 phút trước đó theo yêu cầu mới; đây là cấu hình dự án, không trích nguyên thời hạn của nhà hàng tham khảo. Thời hạn đã lưu của lịch cũ không bị kéo dài/reset ngầm; lịch mới hoặc lịch cũ lần đầu áp dụng chính sách nhận mốc 60 phút. Không cho báo chuyển khoản sau khi đã hết hạn mà chưa từng báo/nhận tiền. Mã lịch mới `DB-XXXXXX`, kiểm tra tồn tại trước khi cấp và vẫn có unique index ở DB; không đổi mã dài của lịch/giao dịch cũ.
- **Tự hủy quá hạn, không xóa yêu cầu:** worker kiểm tra khi khởi động và mỗi phút. Lịch tự tính cọc còn `ChoCoc`, đã qua hạn, chưa báo chuyển khoản và chưa ghi nhận cọc được chuyển `DaHuy`, lưu lý do/thời điểm, rời danh sách chờ xác nhận. Giữ mã, món nháp, liên kết bàn và lịch sử để truy vết; không đổi bàn vật lý sang trống/cần dọn vì đây là lịch tương lai chưa nhận khách. Lịch có tiền, hóa đơn đã thu hoặc món đã chế biến không tự hủy; lịch cọc thủ công cũ không tự áp quy tắc mới.
- **Đã báo chuyển đúng hạn:** tiếp tục giữ chỗ tạm trong lúc Thu ngân đối chiếu, kể cả qua mốc 1 tiếng; chưa chốt lịch cho tới khi tiền được xác nhận. Các truy vấn xếp bàn/sơ đồ/giữ chỗ đều tính lịch này là còn giữ để không xếp khách khác đè lên. Thu ngân vẫn xác nhận được sau hạn báo chuyển nếu lịch phục vụ còn hiệu lực. Không hủy tay khi thông báo tiền chưa xử lý. Nếu thu ngân từ chối và đã hết hạn, worker chuyển sang Đã hủy; không cho khách chuyển thêm vào lịch đã hết hạn. Khách đã chuyển nhưng quên báo trước hạn cần liên hệ nhà hàng đối chiếu/hoàn hoặc lập lịch mới, không tự mở lại bàn đã nhường cho người khác.
- **QR và đối chiếu:** Admin vào Vận hành → Ảnh QR nhận cọc (`/CauHinhQr`), tải PNG/JPG tối đa 2 MB để thay ảnh ngay trên web. Chỉ Admin được mở/lưu, có CSRF, kiểm tra chữ ký loại ảnh và chặn phiên bản cũ ghi đè. Không thêm bảng/cột ngân hàng, không bắt nhập STK hay sửa code mỗi lần đổi ảnh. Ảnh được lưu ở `RestaurantManagement.Web/App_Data/payment-qr`, không commit ảnh người dùng vào Git; mỗi máy/server cần tải ảnh riêng hoặc sao lưu thư mục này khi triển khai. File ảnh theo hash được giữ để trang khách đang mở vẫn thấy đúng ảnh phiên bản cũ. Nếu chưa tải ảnh: Development dùng QR SVG tượng trưng ghi rõ không chuyển tiền thật; production không có hướng dẫn thanh toán QR. Có ảnh thì trang cọc dùng ảnh đó ở cả hai môi trường, hiển thị số cọc/mã đặt bàn riêng; QR ảnh tĩnh không tự điền số tiền/nội dung, không giải mã/xác minh tài khoản trong ảnh hay kết nối ngân hàng. Admin phải kiểm tra QR đúng nơi nhận tiền; nếu đổi tài khoản ngân hàng cần nhân viên đối chiếu các khoản đang chờ. Khách bấm Tôi đã chuyển khoản chỉ báo chờ đối chiếu, chưa ghi nhận tiền/chốt lịch. Thu ngân/Admin kiểm tra tiền thực vào và mã lịch rồi xác nhận; đủ cọc mới chốt. Vẫn giữ người xử lý/số tiền/thời điểm/chống gửi lặp, không bắt nhập mã ngân hàng hoặc tự ghi tiền từ ảnh QR. Mã tham chiếu cũ vẫn giữ, nếu có gửi thì chặn dùng lại. Không khớp thì gửi lý do; trang khách tự kiểm tra trạng thái mỗi 10 giây. Chưa có callback tự xác nhận hoặc tự hoàn tiền qua ngân hàng.
- **Món bổ sung, hủy và hóa đơn:** thêm món trước nhận bàn tính lại phần cọc còn thiếu sau khi trừ khoản đã thực nhận; không thu lại cọc gốc, không sửa món đã gửi bếp. Đang chờ đối chiếu khoản chuyển thì tạm chặn sửa món để giữ đúng số tiền cần xác nhận. Sau nhận bàn, gọi thêm là món trên hóa đơn bình thường, không yêu cầu cọc mới. Hủy vẫn do nhà hàng đối chiếu món đã chuẩn bị và tiền; Admin xử lý hoàn/giữ thực tế theo thỏa thuận, có lịch sử. Hóa đơn cuối chỉ cấn trừ tiền đã được xác nhận, không cấn trừ tiền khách tự báo. Tiền chuyển vào lịch đã hết hạn cần được nhà hàng đối chiếu/giải quyết trước khi lập lịch mới, không tự chốt lại bàn đã giải phóng.
- **Nguồn và mức áp dụng:** [CoCo Saigon](https://cocosgn.com/vi/dat-ban/) công bố cọc 50% giá trị set menu cho nhóm thuộc diện áp dụng; [Nhà Bè Khánh Hào](https://nhabekhanhhao.com/en/chinh-sach-dat-cho) công bố 299.000đ/lịch giữ bàn cả nhóm, đối chiếu đúng giao dịch rồi mới xác nhận. Dự án học cách tính và đối chiếu từ các nguồn này; không tuyên bố mọi nhà hàng áp dụng cùng mức, không sao chép toàn bộ chính sách hoàn/hủy hoặc thời hạn đặt tiệc vào khách dùng bữa trong ngày. Không dùng 50.000đ/khách: ở nguồn NBKH mức đó thuộc vé cano, không phải cọc bàn.
- **Hủy khi đã có món/tiền:** khách gửi yêu cầu để nhà hàng xử lý, tạm dừng gửi bếp. Hủy thông thường chặn khi còn tiền cọc chưa giải quyết, món đang nấu/đã ra hoặc hóa đơn đã thanh toán. Admin có bước đối chiếu và đóng lịch trước khi khách nhận bàn; giữ lịch sử món đã chế biến, tiền đã thu/hoàn/giữ, không tự ghi khoản chưa thu là đã thanh toán và không tự hoàn nguyên liệu đã dùng. Hóa đơn thu một phần phải được thu ngân đối chiếu trước.
- **Tiếp tân và sơ đồ:** chọn ngày/giờ ngay trên sơ đồ khu vực/tầng; mỗi ô bàn hiển thị lịch bắt đầu–kết thúc trong ngày, phân biệt xung đột với khoảng đang xem. Xếp bàn vẫn kiểm tra sức chứa, khu vực và lịch chồng ở server. “Món & cọc” trong danh sách tiếp nhận mở cùng chi tiết lịch, không phải một bộ dữ liệu đặt bàn khác.
- **Tối ưu diện tích:** POS chọn bàn rồi thu danh sách bàn thành thanh tóm tắt; thực đơn và giỏ mở rộng ngay trong trang. Giỏ cuộn riêng; giữ món khi đổi qua lại các bàn. Sidebar có nút ẩn/hiện trên desktop và hành vi phù hợp điện thoại. Web khách vẫn giữ UI của nhóm, nhân viên xem được nội dung nhưng không có nút đặt bàn của khách.
- **Giỏ món 09/10:** chọn size, số lượng, ghi chú rồi bấm Thêm vào giỏ; đóng khung chọn món và tiếp tục xem thực đơn, không tự bật giỏ. Giỏ mở khi khách chủ động bấm; có thông báo thêm món và chuyển động nhẹ, tôn trọng tùy chọn giảm chuyển động. Món không có ảnh dùng toàn bộ chiều rộng thay vì bị ép vào cột ảnh. Giữ món/ghi chú khi đăng nhập, kể cả tài khoản có giỏ rỗng lưu từ lần trước; giỏ thuộc lịch khác không bị nhập nhầm. Bỏ nhãn nội bộ “HÌNH ẢNH MINH HỌA” khỏi trang chủ.
- **Thông báo cọc 09/10:** lịch online mới hiện số cọc và bước thanh toán ngay; phân biệt Chờ thanh toán, Chờ đối chiếu, Đã xác nhận, Hết hạn chờ worker xử lý và Đã hủy. Nút Kiểm tra trạng thái dùng AJAX chung với tự kiểm tra mỗi 10 giây: còn chờ thì hiện giờ kiểm tra, thay đổi thì tải thông tin mới, lỗi mạng/phiên đăng nhập thì báo rõ và cho thử lại. Lịch đã hủy hiện lý do và không có QR/nút thanh toán hay nút kiểm tra vô hiệu. Không tự xác nhận tiền và không bắt tiếp tân nhập mức cọc cho lịch mới. Các lịch cũ có thỏa thuận thủ công giữ nguyên; chưa có số tiền thì có bước áp dụng chính sách, không sửa ngầm dữ liệu. Chưa có tệp đề cương riêng của nhà hàng để đối chiếu ngoài ảnh góp ý/phân công đã gửi.
- **Demo:** lệnh Development `--init-week8-demo` tạo khu vực Sảnh gia đình và ba bàn đang phục vụ cùng lúc, liên kết hồ sơ khách/tiếp tân của bộ demo. Tên “Minh họa tuần 8”/ghi chú kỹ thuật cũ được đổi có kiểm tra trên đúng bộ dữ liệu này; giữ mã và liên kết lịch/bàn. Chạy lại không nhân bản hoặc mở lại lượt phục vụ đã kết thúc. Không tự nạp demo khi mở web bình thường.
- **Điểm nối code:** `DatBanController` phục vụ khách sở hữu lịch; `DatTruocController` xử lý món/cọc nội bộ; `PreorderService` giữ quy tắc gửi món, phiên bản và cọc; `TableService` nhận/xếp/kết thúc/dọn bàn; `OrderService` hóa đơn và đối trừ. Migration `Week8PreordersAndDepositLedger` bổ sung trường và bảng giao dịch, giữ dữ liệu lịch cũ. Sao lưu trước khi nâng database đang dùng; không rollback migration sau khi có giao dịch cọc mới nếu chưa xuất/sao lưu lịch sử.
- **Điểm nối thanh toán mới:** `ReservationDepositPolicy` là công thức chung; `TableService.ReserveOnlineWithinTransaction` giữ chỗ trước khi thu tiền. `ThanhToanCoc`/`BaoChuyenKhoan` chỉ cho khách sở hữu lịch; `/DatTruoc`/`GiaoDichCoc`/`TuChoiChuyenKhoan` cho Thu ngân/Admin. Hai migrations `AutomaticReservationDeposits`, `PaymentNoticeReconciliation` chỉ thêm trường, mặc định lịch cũ không chuyển sang tự tính. Không sửa khoản đã thu hoặc mã cũ khi đổi chính sách. Nếu đang có yêu cầu hủy mà tiền vẫn thực nhận, ghi nhận tiền để xử lý hoàn/giữ nhưng không chuyển lịch sang Đã xác nhận.
- **Kiểm chứng 09/10:** build Web/bộ smoke 0 lỗi, 0 cảnh báo; mốc toàn luồng trước phần Admin tải QR đạt **787 kiểm tra HTTP/database** trên SQL LocalDB trong database GUID riêng đã tự dọn. Có ca sở hữu/CSRF, giá/snapshot/FIFO, kho/hóa đơn, cọc 50%/299.000đ, mã ngắn, giữ bàn trước thu tiền, xác nhận không cần mã ngân hàng và chống gửi lặp. Ca mới kiểm tra hạn 60 phút, lịch chưa đến hạn không bị hủy, hết hạn chưa báo/nhận tiền tự hủy và giữ lịch sử, mất khỏi danh sách chờ, không đổi trạng thái bàn vật lý, hủy lặp không đổi lịch sử, trang hủy không hiện QR; lịch đã báo chuyển đúng hạn vẫn giữ chỗ và xác nhận được sau hạn nếu khung phục vụ còn hiệu lực; không hủy tay khi chưa đối chiếu, không hủy lịch đã nhận tiền hoặc thỏa thuận cọc thủ công. `TableExpirySmoke` kiểm tra mốc theo giờ nhận thực tế, chưa đủ 3 tiếng, bàn ghép, còn nợ/món/cọc chưa xử lý, dọn từng bàn, giữ hóa đơn đã trả, hủy hóa đơn rỗng và thao tác đồng thời. JavaScript giỏ khách/POS/AJAX/trạng thái cọc đạt. Worker thật đã chuyển ba lượt W8 cũ quá 3 tiếng sang Cần dọn, giữ lịch sử và hủy đúng hóa đơn rỗng 0đ. Đã thử trình duyệt trước phần QR chọn liên tục 10 món/giữ giỏ sau đăng nhập; phần QR/thời hạn mới được kiểm chứng bằng HTML và HTTP/SQL, chưa kiểm trực quan lại. Database demo riêng `RestaurantMember1Refresh_20261009`; không lấy số ca test làm phần trăm hoàn thành nhiệm vụ Người 2/3. Phần Admin tải QR bổ sung: build 0 lỗi/cảnh báo và PASS **45 kiểm tra QR HTTP/database** tập trung trên database/thư mục GUID riêng đã dọn; gồm upload lần đầu/thay ảnh, quyền/CSRF, ảnh giả/quá lớn, phiên bản cũ, giữ ảnh cũ và trang cọc dùng ảnh mới nhưng không ghi nhận tiền. Chưa kiểm trực quan lại bằng công cụ trình duyệt; Admin tải ảnh QR trên máy/server đang triển khai. Ảnh tải lên là cấu hình riêng, không đi theo Git; người dùng đã yêu cầu bàn giao phần code tải QR trên cùng nhánh.

Không mở rộng phần Người 1 sang công thức size, tự xuất nguyên liệu, chương trình khuyến mãi hoặc báo cáo. Các điểm đó cần người phụ trách nối theo mục 5, dùng đúng snapshot/luồng gọi món hiện có.

### Tài khoản và phân quyền

- Đăng nhập, đăng xuất, đổi mật khẩu; từ chối sai mật khẩu, tài khoản khóa/ngừng sử dụng, nhân viên nghỉ việc và truy cập chưa đăng nhập. Giữ khóa tạm 15 phút sau 5 lần sai mật khẩu.
- Gộp cấp tài khoản, sửa thông tin đăng nhập, khóa/mở khóa và đặt lại mật khẩu vào hồ sơ **Nhân viên**. Không còn hai màn hình chọn lại quyền riêng cho cùng một nhân viên.
- Mỗi nhân viên có **một vai trò công việc**, ánh xạ sang một vai trò Identity với bộ quyền cố định. Admin đổi vai trò trong hồ sơ; phiên cũ bị thu hồi và nhân viên phải đăng nhập lại. Server không nhận quyền tùy ý từ dữ liệu gửi lên.
- Khách được sửa thông tin của mình; Admin sửa thông tin của mình và tài khoản khách/nhân viên được quản lý. Nhân viên không tự sửa hồ sơ hay quyền, nhưng vẫn tự đổi mật khẩu.
- Admin quản lý tài khoản khách: tìm kiếm, sửa thông tin, khóa/mở khóa, đặt lại mật khẩu. Không đổi khách thành nhân viên hoặc xóa lịch sử của khách.
- Sửa thông tin nhạy cảm/đặt lại mật khẩu yêu cầu mật khẩu xác nhận của người thao tác. Không xem mật khẩu cũ; đặt lại thu hồi phiên cũ và **không tự mở khóa**. Tài khoản Admin khác không nằm trong luồng đặt lại mật khẩu này.
- Kiểm tra quyền thật ở controller/server, không chỉ menu. Các trang nhân viên hiển thị nhãn vai trò nhỏ; đăng nhập chuyển tới đúng phần việc.

### Sơ đồ bàn và tiếp nhận đặt bàn

- `/SoDoBan` hiển thị ô bàn theo khu vực/tầng, sức chứa, trạng thái hiện tại và lịch trùng trong khoảng đang xếp. Cập nhật mỗi 10 giây; không thêm framework realtime khi chưa cần.
- Tiếp tân/Admin xem yêu cầu chờ và thông tin yêu cầu đang chọn cạnh sơ đồ; trên màn hình nhỏ có nút tới yêu cầu/sơ đồ, không cần đổi trang mất thông tin. Panel chờ hiển thị tối đa 30 yêu cầu gần nhất theo giờ đến; danh sách đầy đủ và lịch sử vẫn ở `/QuanLyDatBan`.
- Bấm mã bàn hoặc **Đặt lịch tại bàn này** để nhập yêu cầu, điền sẵn bàn/khu vực. Lưu chỉ tạo `ChoXacNhan`, chưa giữ bàn; bàn hợp lệ được chọn dự kiến trên sơ đồ, vẫn cần bấm xác nhận. Nếu lịch thay đổi, chọn bàn khác; nhóm đông có thể chọn thêm bàn cùng khu vực.
- Gợi ý một bàn đơn còn lịch phù hợp, đủ sức chứa/VIP; ưu tiên khu vực yêu cầu rồi số chỗ ít dư nhất. Với lịch sắp đến, chỉ gợi ý bàn đang sẵn sàng. **Chọn bàn gợi ý** không tự duyệt. Ghép bàn/vị trí đặc biệt do tiếp tân quyết định; chưa có dữ liệu để tự suy vị trí cửa sổ hoặc bàn sát nhau. Server kiểm tra lại khi xác nhận, không tin gợi ý cũ.
- Admin cấu hình khu vực/tầng và bàn vật lý. Tiếp tân **không tạo bàn vật lý**: nhận yêu cầu qua điện thoại/tại quầy, nhập thông tin khách rồi xếp vào các bàn đã có. Khách vãng lai không bị ép tạo tài khoản.
- Khách đã đăng nhập gửi yêu cầu ngày giờ, số người, khu vực ưu tiên. Server gắn đúng khách hiện tại; lịch sử, xác nhận gửi yêu cầu và đánh giá chỉ truy cập đặt bàn thuộc khách đó, không suy quyền từ số điện thoại/mã đặt bàn gửi lên.
- Tiếp tân/Admin chọn một hoặc nhiều bàn cùng khu vực: kiểm tra sức chứa, VIP, lịch chồng nhau và trạng thái sử dụng. Xác nhận/xếp/nhận/hủy dùng chung logic, transaction và kiểm soát phiên bản dữ liệu; hai yêu cầu đồng thời không chiếm cùng bàn cùng lịch.
- Nhận khách: lịch đã xác nhận, đã xếp bàn, trong thời gian nhận và bàn sẵn sàng; các bàn ghép chuyển sang `DangPhucVu`. Hủy một lịch tương lai không giải phóng bàn của lượt khách đang phục vụ.
- **Thời lượng và kết thúc:** lịch online mới dự kiến 3 tiếng; form tiếp tân mặc định 180 phút, cho nhập 30–180 phút và server chặn vượt 180. Không sửa ngầm giờ/thỏa thuận của lịch đã lưu. Mốc quá hạn phục vụ tính từ `ThoiDiemNhanBan + 3 tiếng`, không từ lúc khách tạo yêu cầu. Lịch cũ thiếu giờ nhận dùng `GioDen` làm mốc dự phòng; sơ đồ hiển thị mốc để nhân viên kiểm tra. Đây là quy ước vận hành đã chốt cho dự án, không tuyên bố mọi nhà hàng giới hạn 3 tiếng.
- **Bồi bàn chủ động bấm Kết thúc phục vụ** ngay khi khách xong, đã thanh toán và xử lý hết món/cọc; không phải chờ đủ 3 tiếng. `TableExpiryWorker` kiểm tra khi web khởi động và mỗi phút; lượt quá 3 tiếng đủ điều kiện được tự kết thúc qua cùng `TableService.FinishWithinTransaction` như thao tác bồi bàn. Chưa trả hóa đơn, còn món chưa phục vụ/hủy hoặc tiền cọc chưa đối trừ/hoàn/giữ thì không tự đóng; sơ đồ báo quá 3 tiếng để nhân viên xử lý. Hóa đơn rỗng 0đ chưa có nghiệp vụ được hủy, không giả thanh toán; hóa đơn đã trả và lịch sử tiền giữ nguyên. Mỗi lượt dùng transaction/khóa cùng thứ tự bàn, kiểm tra lại lượt hiện tại; không giải phóng nhầm lượt mới khi job trùng thao tác nhân viên.
- Chỉ Admin/Bồi bàn thao tác kết thúc/dọn bàn cả trên UI và server; Tiếp tân tạo, xếp và nhận khách, không dùng thao tác phục vụ này.
- Kết thúc phục vụ chuyển toàn bộ bàn ghép sang `CanDon`; từng bàn bấm Dọn xong mới về `SanSang`. Danh sách đặt bàn chuyển lượt đã kết thúc sang lịch sử, không còn ghi đang phục vụ.
- Nếu lượt khách chưa gọi món, không có tiền/chi tiết món phải thu, cho phép kết thúc và hủy bill trống; không ép tạo giao dịch thanh toán giả.
- Migration mới thêm tầng, khu vực ưu tiên và thời điểm kết thúc lượt phục vụ, không xóa bảng/dữ liệu cũ. Chỉ khép lịch sử cũ đủ điều kiện rõ ràng: quá giờ, bill đã trả/hủy và không còn bàn đang phục vụ; dữ liệu đang phục vụ không bị tự khép vì đã trả tiền.

### Phân chia thực đơn — chốt ngày 02/10

- **Bỏ hai role riêng `ThucDon` và `DanhMucMon`**. Chỉ có 6 vai trò nội bộ gồm Admin, Tiếp tân, Phục vụ, Thu ngân, Quản lý bếp, Kho; thêm Khách hàng là 7 role đăng nhập.
- `Bep` là **Quản lý bếp/điều phối**, không chỉ người trực tiếp nấu. Có hai màn hình tách biệt: `/Bep` chỉ các món đang gọi (không tiền, không danh sách toàn thực đơn); `/MonAn` chuẩn bị món lẻ/thức uống, nội dung kỹ thuật, size và công thức cơ sở.
- **Admin** quản lý danh mục, giá từng size, combo và trạng thái kinh doanh. Bếp không tạo/sửa combo, xóa món, đặt giá hoặc tự duyệt mở bán. Không chỉ ẩn ô: server bỏ qua giá/trạng thái/duyệt do Bếp giả gửi lên; endpoint xóa/danh mục và combo bị chặn thật.
- Bếp tạo/sửa → `DaDuyet=false` → Admin kiểm tra, nhập giá các size đang dùng > 0 và chọn **Duyệt mở bán**. Bếp sửa món đã duyệt thì cần duyệt lại; giữ giá cũ nhưng tạm ẩn món khỏi web khách. Dòng món/giá chốt trên bill cũ không bị sửa theo thực đơn.
- `DaDuyet` và `TrangThai` là hai việc khác nhau: tạo mới luôn `DangPhucVu`, nhưng món chưa duyệt **chưa được mở bán**. Danh sách quản lý có nhãn/bộ lọc Chờ duyệt, không tính món chờ duyệt vào số món đang phục vụ.
- Trang chủ và URL chi tiết công khai đều kiểm tra duyệt, danh mục, trạng thái và giá hợp lệ. Combo có món thành phần chưa duyệt/ngừng kinh doanh cũng không được hiển thị công khai. TV2 phải giữ cùng điều kiện khi ghi order, không tin việc món đã có trên màn hình từ trước.
- Cho đổi sang `TamHet` dù món còn nằm trên hóa đơn chưa thanh toán: không sửa/hủy dòng món, giá chốt, trạng thái bếp hay trạng thái thanh toán cũ. Chặn nhận order mới cho món tạm hết là trách nhiệm của luồng gọi món tiếp theo.
- Chuyển `NgungKinhDoanh` phải nhập lý do; chỉ Admin thao tác. Server còn chặn khi món có dòng chưa hủy trên bill chưa thanh toán/thanh toán một phần; không đồng nhất điều kiện này với `TamHet`. Combo chọn món lẻ có sẵn, không nhập lại nguyên liệu thô. Công thức theo size và size của món thành phần combo **chưa hoàn tất**, xem bàn giao ở mục 5.
- Quyết định này thay yêu cầu cũ “chỉ Admin chuẩn bị toàn bộ thực đơn”: Admin vẫn quyết định kinh doanh/giá; Quản lý bếp chuẩn bị chuyên môn. Nhóm cần cập nhật đặc tả/ma trận quyền theo hướng đã chốt, không khôi phục hai role cũ.

## 3. Vai trò hiện tại

| Vai trò | Phần được dùng/quản lý | Không được làm |
| --- | --- | --- |
| Admin | Quản lý nhân viên/tài khoản khách, thực đơn/danh mục, cấu hình bàn/khu vực, giám sát nghiệp vụ và xem/in bill | Không xem mật khẩu cũ; không tự đóng dấu thanh toán online thành công |
| Khách hàng | Trang công khai, tài khoản của mình, gửi/xem yêu cầu đặt bàn của mình, đánh giá đủ điều kiện | Không vào quản trị hoặc xem lịch của khách khác |
| Tiếp tân | Nhận yêu cầu đặt bàn, tạo yêu cầu cho khách, xếp/nhận/hủy lịch và sơ đồ bàn | Không tạo bàn vật lý, sửa thực đơn/giá hoặc thu tiền |
| Bồi bàn | Sơ đồ bàn, kết thúc phục vụ và xác nhận dọn xong | Không sửa cấu hình bàn, giá, công thức hoặc xác nhận thu tiền |
| Thu ngân | Xem/in bill, xác nhận tiền mặt qua luồng hiện có | Không sửa giá thực đơn/công thức; không xác nhận online chỉ từ nút bấm/ảnh chuyển khoản |
| Quản lý bếp (`Bep`) | KDS món đang gọi; màn hình riêng chuẩn bị thông tin món lẻ/thức uống, size, công thức cơ sở và gửi duyệt | Không tiền trên màn hình bếp, không đặt giá/duyệt/xóa món, không CRUD danh mục/combo |
| Kho | Danh mục nguyên liệu, ngưỡng cảnh báo, xem tồn từ chứng từ và cảnh báo Cần bổ sung tại danh sách | Không tự quyết định công thức món hoặc giá bán |

### Những phần chỉ Admin quản lý — không phải đang thiếu người phụ trách

| Phần chỉ Admin được sửa | Lý do và ranh giới với nhân viên |
| --- | --- |
| Nhân viên, tài khoản khách, khóa/mở khóa và đặt lại mật khẩu | Kiểm soát quyền và thông tin tài khoản; nhân viên chỉ tự đổi mật khẩu, khách tự sửa hồ sơ của mình |
| Danh mục món | Thống nhất cách phân loại. Bếp chọn danh mục đã có khi chuẩn bị món, không cần một nhân viên/role Danh mục riêng |
| Giá theo size, combo, duyệt mở bán, ngừng kinh doanh/xóa món | Quyết định kinh doanh. Bếp làm thông tin kỹ thuật, size và công thức rồi gửi duyệt; không được sửa giá |
| Thêm/sửa/xóa bàn vật lý, khu vực và tầng | Cấu hình nhà hàng. Tiếp tân xếp khách vào bàn có sẵn; Bồi bàn xử lý phục vụ/dọn bàn, không tự tạo bàn vật lý |

Bộ quyền cố định theo vai trò công việc, không có bộ quyền thứ hai để chọn lại. Không thêm role hoặc mở quyền chỉ để chia bớt menu của Admin. Admin giám sát không đồng nghĩa làm thay mọi thao tác: xác nhận thu tiền mặt hiện thuộc Thu ngân; công thức thuộc chuyên môn Quản lý bếp; chứng từ và tồn thuộc Kho. Chưa có chức năng gọi món/nhập–xuất hoàn chỉnh là phần cần phát triển, không phải quyền đã giao hết cho Admin.

## 4. Tài khoản để nhóm chạy thử

Sáu tài khoản nội bộ từ SQL của nhóm gồm Admin và 5 loại nhân viên vẫn dùng mật khẩu riêng: xem **README.md**. Seed không còn tạo hai tài khoản thực đơn/danh mục. Pull code không mang theo database; mỗi máy phải cấu hình connection string và nạp dữ liệu/khởi tạo tài khoản trên đúng database local.

Nếu cần bộ demo đủ cả Khách và 6 vai trò nội bộ, chạy từ thư mục repo sau khi cấu hình database:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:AuthBootstrap__DemoPassword='Demo@2026!'
dotnet run --project RestaurantManagement.Web -- --init-demo-accounts
dotnet run --project RestaurantManagement.Web --launch-profile http
```

| Vai trò | Email đăng nhập | Mật khẩu demo công khai |
| --- | --- | --- |
| Admin | `admin.demo@example.test` | `Demo@2026!` |
| Khách hàng | `khach.demo@example.test` | `Demo@2026!` |
| Tiếp tân | `tieptan.demo@example.test` | `Demo@2026!` |
| Bồi bàn | `boiban.demo@example.test` | `Demo@2026!` |
| Thu ngân | `thungan.demo@example.test` | `Demo@2026!` |
| Quản lý bếp | `bep.demo@example.test` | `Demo@2026!` |
| Kho | `kho.demo@example.test` | `Demo@2026!` |

Lệnh chỉ chạy ở Development, không tự chạy khi mở Web. Chạy lại không đổi mật khẩu đã có hay ghi đè hồ sơ trùng/sai; nếu tài khoản đã đổi mật khẩu, dùng mật khẩu mới hoặc luồng Admin đặt lại. Không dùng các mật khẩu công khai này khi triển khai thật.

## 5. Bàn giao còn thiếu — đối chiếu code, không làm lại phần đã có

**Phân công mới đang áp dụng:** Người 1 = đặt bàn/trải nghiệm khách/tối ưu UI/AJAX; Người 2 = món, định mức, kho và cung ứng; Người 3 = nhân sự/phân quyền, khuyến mãi và thống kê. POS/KDS/thanh toán là nền nhóm đã có từ phân công trước, không phải yêu cầu viết lại hoặc vẫn giao kho cho Người 3. Mục 7–8 lưu các mốc kiểm chứng cũ; bảng này và mục 6 mới là hướng dẫn tiếp nối hiện tại.

| Phần | Đã có trên bản nhóm/nhánh này | Cần tiếp tục |
| --- | --- | --- |
| Trang khách/bàn/UI — Người 1 | Giữ UI nhóm; giỏ có ảnh/size/số lượng/ghi chú, chọn trước hoặc bổ sung sau đặt bàn; cọc 50% món hoặc 299.000đ/lịch; QR demo, báo chuyển và thu ngân đối chiếu không nhập mã ngân hàng; hạn mới 1 tiếng, tự hủy lịch chưa báo/nhận cọc; lịch đã báo chuyển được giữ chờ đối chiếu; mã ngắn; sơ đồ khu vực/tầng có khung giờ, bảng POS mở rộng, menu thu gọn; sau 3 tiếng tự kết thúc lượt đủ điều kiện, dọn xong mới sẵn sàng; AJAX trên các luồng đã nối | Người 1 kiểm trực quan lại toàn luồng ở nhiều tab/tài khoản; các form mới do Người 2/3 bổ sung phải nối cùng AJAX, không mặc định mọi form hiện có đã dùng AJAX. Ngân hàng thật và nội dung/ảnh nhà hàng là bước triển khai sau, không cản demo hiện tại |
| Nguyên liệu theo size — phần thực đơn đợt trước | Đã có món, các size/giá và công thức chung của món; Bếp chuẩn bị món, Admin duyệt bán | Chưa nhập/lưu lượng nguyên liệu riêng cho size nhỏ, vừa, lớn. Cần bổ sung như ví dụ ngay dưới bảng |
| Size của món trong combo | Đã chọn được món lẻ, số lượng và giá bán trọn combo | Chưa chọn được size của từng món trong combo. Cần bổ sung như ví dụ ngay dưới bảng |
| Trạng thái/nhãn món | Tạo `DangPhucVu`, duyệt mở bán riêng, `TamHet` không đổi order cũ, ngừng kinh doanh phải có lý do | Món mới/nổi bật chưa có tiêu chí chính thức: cần chốt số ngày “mới”, tiêu chí bestseller/đặc sản; chưa tự thêm thuật toán xếp hạng |
| Gọi món — nền nhóm | Đã có màn hình và logic POS `/DonHang/Create` và `/DonHang/AddDishes`: tại bàn/mang đi/giao hàng, snapshot `TenSizeLucBan`, `ThoiDiemGoi`, ghi chú và giá; kiểm tra duyệt/mở bán và tính hóa đơn ở server | Người 2 nối dữ liệu định mức/size combo vào phần đang có; giữ chống gửi lặp, FIFO từng lần gọi và một hóa đơn hợp lệ của lượt. Không cần làm app Mobile mới |
| Bếp & Phục vụ — nền nhóm | KDS `/Bep` FIFO theo từng dòng order; size/loại đơn/ghi chú; Bếp `ChoCheBien` → `DangCheBien` → `SanSang`; Bồi bàn xác nhận mang ra `DaPhucVu`; hủy món chưa nấu điều chỉnh bill | Đã có bước Đã phục vụ, không giao lại như chức năng chưa làm. Người 2 nối kho đúng một lần theo thời điểm gửi món sau nhận bàn, giữ bước xác nhận mang ra thật và không hoàn nguyên liệu món đã nấu |
| Thanh toán — nền nhóm | `/HoaDon/Details`/`ThanhToan` có tiền mặt, giao diện QR/thẻ, ghi nhận thanh toán/chiết khấu, cấn trừ cọc đã nhận, in bill. Đây là giao diện và ghi nhận do nhân viên, không chứng minh đã kết nối ngân hàng/POS thật | Người 3 nối voucher/điều kiện giảm giá và báo cáo vào hóa đơn hiện có; không làm lại màn thanh toán hay biến khách báo chuyển cọc thành tiền thực nhận. Webhook/ngân hàng thật chỉ làm khi có tài khoản và phạm vi triển khai |
| Món/định mức/kho — Người 2 | Đã có CRUD, nhà cung cấp, chứng từ nhập/xuất/thanh lý và báo cáo kho của nhóm; công thức món hiện chưa tách từng size | Nối nhà cung cấp–cung ứng nhiều/nhiều; dấu + tạo nhanh danh mục/nguyên liệu/nhà cung cấp giữ form; định lượng mỗi size, size từng thành phần combo; trừ kho đúng một lần khi món sau nhận bàn được gửi thực sự, không trừ khi khách chỉ bỏ vào giỏ hoặc chọn trước. Giữ chặn đổi đơn vị nguyên liệu đã dùng; không hoàn kho món đã nấu khi hủy |
| Nhân sự/khuyến mãi/thống kê — Người 3 | Ma trận role, quản lý tài khoản/hồ sơ nhân viên và hóa đơn đã có; CSDL có `KhuyenMai`, `KhuyenMaiMon`, `Voucher`, điều kiện/giới hạn/kết hợp; trang khách đọc chương trình hiện hành. Báo cáo nhập–xuất–tồn của nhóm đã có | Không viết lại auth hoặc coi đã có bảng khuyến mãi là đã hoàn tất nghiệp vụ. Nối quản trị chương trình/voucher và áp dụng/giới hạn lượt/kết hợp ở server; quyền theo chức vụ dựa trên ma trận hiện có; báo cáo doanh thu ngày/tháng/năm, đơn, bestseller và hiệu quả ưu đãi. Không cộng cọc chưa đối chiếu/hóa đơn hủy vào doanh thu; không cộng cọc hai lần khi đã trừ trên bill |
| Báo cáo Word — cả nhóm | Luồng/vai trò hiện tại ghi trong file này | Cập nhật sơ đồ, đặc tả, ma trận quyền, luồng bàn/bếp, công thức giá combo và các trạng thái theo code thật; không ghi phần bàn giao còn thiếu là đã làm |

### Hai góp ý thực đơn cần làm tiếp — giải thích bằng ví dụ

**Nguyên liệu theo size:** Cùng một món nhưng phần nhỏ và phần lớn sẽ dùng lượng nguyên liệu khác nhau. Ví dụ Gỏi size S dùng 100g tôm, size L dùng 200g tôm. Hiện code mới lưu công thức chung cho món Gỏi, chưa phân biệt lượng theo size. Người tiếp tục phần thực đơn cần sửa form: chọn các nguyên liệu của món một lần, rồi nhập lượng riêng cho từng size; bấm Lưu món mới lưu toàn bộ. Bếp nhập size/công thức, Admin nhập giá và duyệt bán. Đây là dữ liệu để sau này tính đúng lượng xuất kho, không phải Kho tự quyết định công thức nấu.

**Size của món trong combo:** Combo phải ghi rõ gồm món gì, size nào và bao nhiêu phần. Ví dụ Combo gia đình gồm 1 Gỏi size L và 2 Canh size S. Hiện code chọn được món và số lượng nhưng chưa chọn size, nên chưa biết phải làm phần nhỏ hay lớn. Người tiếp tục phần thực đơn cần thêm lựa chọn Món → Size → Số lượng cho từng dòng combo. Không nhập lại nguyên liệu vì đã lấy từ công thức của món/size được chọn; giá bán trọn combo vẫn do Admin đặt, không bắt buộc bằng tổng giá từng món lẻ.

Hai mục này là **phần thực đơn của Người 2 chưa hoàn tất theo góp ý của cô**, không phải yêu cầu làm lại tài khoản/phân quyền hoặc sơ đồ bàn của Người 1. Kho/cung ứng cũng thuộc Người 2 theo phân công mới; Người 3 làm nhân sự/khuyến mãi/thống kê. Phải bổ sung cách lưu dữ liệu, không chỉ thêm ô trên giao diện; công thức/combo cũ thiếu size cần được kiểm tra và chọn lại, không tự đoán.

### Hướng dẫn triển khai cho người hoặc AI nhận phần thực đơn

**Trạng thái hiện tại:** Đây là yêu cầu cần làm tiếp, không phải mô tả tính năng đã hoàn thành. Đã có size, giá theo size và duyệt mở bán. Chưa có lượng nguyên liệu theo từng size, chưa có size của từng món trong combo. Không ghi “đã xong” hai mục này khi chỉ mới chỉnh form hoặc sửa tài liệu.

**Duyệt mở bán, không phải “duyệt giá” riêng:** Bếp tạo/sửa món thì món chuyển về Chờ duyệt. Admin nhập hoặc kiểm tra giá của các size rồi chọn Duyệt mở bán cho món. Hệ thống kiểm tra giá hợp lệ, không tự đánh giá giá đó đắt/rẻ hay tạo một quy trình xin duyệt giá riêng. Cách mô tả đúng là: “Đã có size, giá từng size và duyệt mở bán; còn thiếu lượng nguyên liệu theo từng size và size của món thành phần combo.”

**1. Đọc đúng chỗ trước khi sửa**

- `RestaurantManagement.API/Models/Entities.cs`: `MonAnSize` đã có tên/giá; `DinhMucMon` hiện lưu `MonAnId + NguyenLieuId + SoLuong`, tức lượng chung cho món; `ChiTietCombo` chỉ có món thành phần và số lượng, chưa có size.
- `RestaurantManagement.API/Data/RestaurantDbContext.cs`: khóa định mức đang theo món/nguyên liệu, khóa combo theo combo/món. Không chỉ thêm thuộc tính rồi quên quan hệ, khóa và các truy vấn dùng khóa cũ.
- `RestaurantManagement.Web/Models/MonAnViewModels.cs`, `Controllers/MonAnController.cs`, `Views/MonAn/Form.cshtml`, `wwwroot/js/dish-form.js`: form hiện có danh sách size, một danh sách định mức chung và danh sách món trong combo. Sửa trên các phần này, giữ giao diện nhóm và cách kiểm tra quyền hiện tại; không tạo ứng dụng hoặc bộ form song song.
- Tìm các chỗ đọc/ghi `DinhMucMon`, `ChiTietCombo`, `ChiTietComboSnapshot` trong seed, trang khách, bill, đặt trước, KDS và kiểm thử. `HomeController` phải hiển thị/kiểm tra size thành phần combo; kiểm tra xóa size/xóa món phải tính quan hệ mới. Người 2 bàn giao cấu trúc đọc mới cho Người 1 (giỏ/đặt trước) và Người 3 (hóa đơn/ưu đãi/báo cáo); không nối trừ kho chỉ vì form đã thêm ô công thức.

**2. Làm nguyên liệu theo size thành một luồng đầy đủ**

- Thông tin chung vẫn gồm tên món, danh mục có sẵn, ảnh và mô tả. Chọn tập nguyên liệu của món một lần; mọi size đang dùng đều có cùng tập nguyên liệu này, nhưng lượng của mỗi nguyên liệu khác nhau.
- Với từng size, nhập tên size và lượng của các nguyên liệu đã chọn. Ví dụ Gỏi S dùng 100g tôm, Gỏi L dùng 200g tôm. Lượng lưu phải theo đơn vị của nguyên liệu: nếu đơn vị là kg thì hai lượng tương ứng là 0,1kg và 0,2kg, không lưu số 100 vào cột đang hiểu là kg. Hiện chưa có hệ thống đổi đơn vị tự động; phải hướng dẫn đơn vị ngay trên form, không tự suy đoán.
- Bếp nhập nội dung/size/công thức, không nhập giá. Admin nhập giá của từng size và duyệt mở bán. Giữ việc Bếp sửa món thì món cần được duyệt lại; giá cũ không bị dữ liệu Bếp gửi lên ghi đè, món chưa duyệt không public.
- Các size đang nhập nằm trong bảng tạm trên form: thêm/sửa/xóa trước khi lưu, chưa ghi từng dòng xuống DB. Khi bấm Lưu món, server kiểm tra toàn bộ rồi lưu món, size và định lượng trong một transaction; lỗi ở một phần thì không giữ lại món hoặc size lưu dở. Tái sử dụng transaction, RowVersion, chống giả mạo form và xử lý ảnh hiện có.
- Dữ liệu đích phải xác định được **mỗi size → mỗi nguyên liệu → lượng sử dụng**. Cập nhật cả model, quan hệ/khóa, migration/snapshot, dữ liệu gửi từ form và truy vấn đọc lại. Tên field/index khi thêm hoặc xóa dòng bằng JS phải đúng để server nhận đủ các size và lượng tương ứng.
- Kiểm tra phía server: size thuộc đúng món; nguyên liệu tồn tại và hợp lệ; lượng của nguyên liệu đã chọn lớn hơn 0; không lặp nguyên liệu trong cùng size; không nhận size ID của món khác; tập nguyên liệu giữa các size đang dùng không bị lệch. Không chỉ kiểm tra bằng JavaScript.

**3. Làm size của món trong combo**

- Chỉ Admin cấu hình combo. Mỗi dòng chọn một món lẻ/thức uống đã có, sau đó chọn size của chính món đó và số lượng nguyên. Ví dụ 1 Gỏi L + 2 Canh S. Không chọn combo làm thành phần của combo khác, không nhập nguyên liệu thô vào form combo.
- Khi đổi món ở một dòng, phải chọn lại size hợp lệ; server từ chối size thuộc món khác, size ngừng dùng, lượng ≤ 0 hoặc món không đủ điều kiện bán. Giá trọn gói của combo do Admin đặt; giá các món lẻ chỉ phục vụ tham khảo nếu cần, không tự thay giá combo thành tổng giá lẻ.
- Lưu đầy đủ món, size và số lượng của từng dòng. Rà lại khóa/truy vấn cũ chỉ theo `MonAnId`: không để việc thay size sửa nhầm dòng hoặc mất thành phần. Nếu cho phép cùng một món với hai size khác nhau thì cả khóa và form phải phân biệt được hai dòng; không mở lựa chọn đó trên UI khi server vẫn coi là dòng trùng.
- Màn hình chi tiết combo phải đọc lại đúng size đã chọn. Người 2 nối cùng cấu trúc này vào giỏ/POS/KDS: combo dùng món gì/size nào; khi định mức size đúng, nhu cầu nguyên liệu lấy từ size thành phần × số phần × số combo, không tạo công thức nguyên liệu riêng trùng cho combo. Xuất kho thật là bước tiếp theo, không trừ ngay khi chỉ thêm vào giỏ.

**4. Giữ dữ liệu cũ và kiểm tra trước khi bàn giao**

- Thêm migration, không chạy lại `InitialSchema.sql`, xóa database hoặc xóa định mức/combo cũ để “làm mới”. Giữ công thức cũ để đối chiếu. Một công thức chung không chứng minh S/L dùng cùng lượng; combo cũ thiếu size cũng không chứng minh phải chọn size đầu tiên. Dữ liệu chưa xác định phải được người phụ trách kiểm tra/chọn lại; không tự đoán rồi dùng để trừ kho.
- Không sửa ngược hóa đơn, giá chốt, đặt trước hoặc chứng từ kho cũ khi cập nhật thực đơn. Bill/KDS đã có snapshot `TenSizeLucBan` và thời điểm gọi; giữ chúng, không thêm bản sao hoặc đọc lại tên size mới thay lịch sử. Giữ snapshot thành phần combo đã chốt. Nếu bỏ cột/bảng cũ sau chuyển đổi, phải xác minh không còn nơi sử dụng và xin chốt riêng.
- Cập nhật seed và kiểm thử theo dữ liệu mới; không xóa ca phân quyền, dữ liệu cũ hoặc tranh chấp chỉ để test đạt. Cả `DbSeeder` và `PopulateRestaurantData.sql` còn ghi công thức/combo kiểu cũ. SQL còn tạo phiếu xuất minh họa từ công thức theo món; đó là dữ liệu demo, không phải tính năng tự trừ kho đang chạy. Khi đổi seed, chạy lại không được đổi chứng từ đã ghi sổ. Kiểm tra tối thiểu: lưu/mở lại Gỏi S và L vẫn đúng lượng khác nhau; sửa lượng S không đổi L; form lỗi không lưu dở; size/nguyên liệu giả bị từ chối; Bếp không đặt giá/duyệt/tạo combo; Admin duyệt rồi khách mới xem được món; combo lưu đúng món/size/số lượng; nâng schema giữ dữ liệu lịch sử.
- Hoàn thành thì cập nhật đúng bảng “Đã có/Cần tiếp tục”, ngày và kết quả kiểm chứng. Ghi rõ Người 1/2/3 cần dùng dữ liệu nào và phần nào chưa tích hợp. Không ghi gọi món/xuất kho đã xong chỉ vì lưu được định lượng và combo; không ghi chương trình ưu đãi đã chạy chỉ vì đã có bảng dữ liệu.

### Thứ tự nối tiếp để demo được trọn luồng

1. **Người 1:** chốt demo đặt bàn/giỏ/cọc/khung giờ, giữ chỗ 1 tiếng, phân biệt chờ chuyển–chờ đối chiếu–đã xác nhận–tự hủy; thử bàn ghép, quá 3 tiếng và dọn bàn. Ngân hàng thật chưa trong phạm vi demo.
2. **Người 2:** làm định lượng theo size và size thành phần combo trước; mở lại form phải đọc đúng dữ liệu. Sau đó nối nhà cung cấp–cung ứng, dấu + tạo nhanh có AJAX và trừ kho đúng một lần khi món sau nhận bàn thực sự được gửi. Không viết lại POS/KDS hoặc bỏ bước bồi bàn Đã phục vụ đang có.
3. **Người 3:** dựa trên auth/role và hóa đơn hiện có để hoàn thiện quản trị ưu đãi, áp dụng voucher/giới hạn lượt/kết hợp và báo cáo. Xác định tiền cọc là khoản đã nhận chờ đối trừ, không tự coi là doanh thu món. Báo cáo kho dùng chứng từ đã ghi sổ, không đọc đơn nháp thành xuất kho.
4. **Cả nhóm:** chạy lại ca tích hợp: chọn trước → cọc → nhận bàn → gửi món → bếp → đã phục vụ → thanh toán/cấn cọc → kết thúc → dọn xong. Giữ FIFO từng lần gọi, giá/size/combo đã chốt và chống thao tác trùng. Cập nhật báo cáo theo dữ liệu thật, không dùng nhãn demo để che chức năng thiếu.

Chưa chốt: số ngày để gọi là món mới, tiêu chí món nổi bật và hình thức thanh toán ngoài tiền mặt sẽ triển khai thật. Chưa làm OTP/MFA hay cơ chế giấu đường dẫn như một lớp bảo mật. Nhóm thống nhất trước rồi cập nhật đúng mục, không thêm tính năng giả để lấp chỗ trống.

## 6. Điểm tích hợp cần giữ

- POS/KDS dùng `DatBan.Id` và các dòng `ChiTietDatBan` để biết bàn của lượt phục vụ; một lượt có thể ghép nhiều bàn. Khách đến trực tiếp dùng yêu cầu nội bộ, không bắt buộc `KhachHangId`.
- Khi nhận bàn, nhân viên có hồ sơ hợp lệ được gắn vào bill theo luồng hiện có. Admin không có hồ sơ nhân viên vẫn nhận bàn được, nhưng không tạo bill với FK nhân viên giả; nhân viên ghi order phải tạo/gắn bill hợp lệ khi khách gọi món. Tránh tạo hai bill chỉ để in hoặc vì refresh trang.
- Chỉ Thu ngân xác nhận đã thu tiền mặt. Bồi bàn kết thúc phục vụ sau khi thanh toán và xử lý xong món; các bàn ghép cùng chuyển cần dọn. Không giải phóng bàn ngay khi Thu ngân bấm thanh toán.
- Dùng cùng `TableService` cho xếp/nhận/hủy/kết thúc bàn, không viết nhánh cập nhật trạng thái riêng bỏ qua transaction/RowVersion. Chuẩn ngày giờ phục vụ theo Việt Nam (UTC+7).
- Các endpoint nghiệp vụ hiện nằm trong Web và được kiểm tra quyền server. Nếu sau này tách API nghiệp vụ, phải cấu hình xác thực/quyền/sở hữu riêng; không coi việc ẩn menu Web là đã bảo vệ API.
- Form mới/tạo nhanh dùng `ajax-forms.js` với `data-ajax-form="true"`, giữ dữ liệu khi lỗi mạng/validation/phiên bản, CSRF và RowVersion. Không tuyên bố tất cả form đang dùng AJAX chỉ vì file JS đã được tải. Dấu + chỉ mở thao tác người hiện tại có quyền ở cả UI và server: không mở quyền danh mục/giá/NCC cho Bếp để tiện tạo nhanh; Bếp vẫn chuẩn bị món/công thức.
- Nhà cung cấp/cung ứng: hiện `PhieuNhap.NhaCungCapId` là một nhà cung cấp cho một phiếu nhập, không phải danh mục quan hệ nhiều–nhiều cung ứng theo góp ý cô. Người 2 đối chiếu bảng Cung ứng trong báo cáo với model thật trước khi bổ sung quan hệ; không biến một phiếu nhập thành nhiều nhà cung cấp hoặc xóa chứng từ cũ để đạt hình thức N–N. Giữ chọn nhà cung cấp cũ và tạo nhanh nhà cung cấp mới theo đúng quyền Kho/Admin, không rời form nhập đang điền.
- Công thức và size thành phần combo là thay đổi dữ liệu liên quan nhiều phần: thống nhất migration, cách giữ lịch sử/chuyển định lượng cũ, seed và kiểm tra trước khi ghép. Bếp cung cấp công thức; không tự mở quyền sửa giá cho Bếp/Kho.
- Khi ghi order, lấy món/size/giá từ DB, kiểm tra `DaDuyet`, `DangPhucVu`, danh mục/size đang dùng, số lượng và thành phần combo. Giữ snapshot tên/size/giá và `ThoiDiemGoi` đã có; đổi thực đơn không cập nhật ngược bill/KDS. Một request lặp không tạo thêm bill/nhân đôi món; món gọi thêm có thời điểm riêng để giữ FIFO.
- Khi Người 2 nối kho, chỉ chứng từ `DaGhiSo` mới làm thay đổi tồn. Xuất kiểm tra lượng khả dụng, đúng đơn vị và lô nhập, không xuất âm khi thao tác đồng thời. Chốt một thời điểm trừ khi món sau nhận bàn thực sự được gửi; không trừ từ giỏ nháp/chọn trước hoặc trừ lần nữa khi bếp đổi trạng thái. Công thức từng size/combo phải đúng trước khi tự động xuất chế biến. Không tạo hệ thống duyệt mua/email mới ngoài yêu cầu cô nếu nhóm chưa thống nhất.
- Chưa bổ sung OTP/MFA, phân quyền tùy biến, engine xếp hạng món hay thanh toán online giả. Chỉ làm khi có nhu cầu/điều kiện triển khai rõ ràng.

### Các điểm code cần đọc trước khi tích hợp

| Điểm giữ chung | File/nhóm file | Tránh sửa đè |
| --- | --- | --- |
| Vai trò, phiên, khởi tạo | `Security/AppRoles.cs`, `ActiveAccountMiddleware.cs`, `AuthSetup.cs`, `Program.cs` | Không khôi phục role cũ hoặc cấp quyền từ checkbox tùy ý; CSS/JS phải tải được trước đăng nhập |
| Cổng khách/nội bộ, hồ sơ | `AccountController`, `AdminController`, `NhanVienController`, `TaiKhoanKhachHangController` | Giữ phân biệt 2 cổng, sở hữu hồ sơ và thu hồi phiên khi khóa/đổi role/reset |
| Menu/UI | `Views/Shared/_ManagementNavigation.cshtml`, `_AdminLayout.cshtml`, `_Layout.cshtml` | Menu và lối tắt dùng chung ma trận; màn hình nhỏ phải còn thấy nội dung, không chỉ menu; không thay UI master bằng khung auth cũ |
| Trạng thái và lịch bàn | `Services/TableService.cs`, `SoDoBanController`, `QuanLyDatBanController` | Dùng service chung/transaction/RowVersion; thanh toán khác với kết thúc phục vụ, lịch tương lai khác trạng thái bàn hiện tại |
| Cọc/hạn giữ chỗ/tự xử lý | `ReservationDepositPolicy`, `PreorderService`, `TableExpiryWorker`, `DatBanController`, `DatTruocController`, `reservation-payment.js` | Hạn mới 60 phút; auto hủy phải khóa/đọc lại lịch, không hủy lịch có thông báo tiền/tiền thực nhận. Xếp bàn giữ lịch đang đối chiếu dù qua hạn. Worker 3 tiếng không tự thanh toán hoặc dọn bàn; giữ lịch sử khi hủy/kết thúc |
| Admin tải ảnh QR | `CauHinhQrController`, `PaymentQrStore`, `Models/PaymentQrViewModel.cs`, `Views/CauHinhQr/Index.cshtml` | Chỉ upload/thay ảnh, không đổi CSDL/README. Giữ quyền Admin, CSRF, kiểm tra PNG/JPG 2 MB, phiên bản và kho file riêng; không biến đổi ảnh thành xác nhận ngân hàng. `PaymentQr:Directory` cho phép đặt thư mục lưu bền vững; kiểm thử dùng thư mục GUID riêng, không thay QR đang chạy |
| Chuẩn bị và bán món | `MonAnController`, `HomeController`, `Models/MonAnViewModels.cs` | Bếp không sửa giá/duyệt; món nháp không public; định mức hiện tại còn theo món, không giả định đã theo size |
| Thanh toán/KDS | `HoaDonController`, `BepController` | Không đổi bill cũ khi sửa món; thanh toán chỉ theo quyền và nguồn đáng tin; món bill hủy không tiếp tục chế biến |
| CSDL | `RestaurantManagement.API/Models/Entities.cs`, `Data/RestaurantDbContext.cs`, `Migrations`, `Scripts` | Đổi model phải có migration/snapshot và cập nhật seed; không chạy lại InitialSchema trên DB đang có dữ liệu để “sửa nhanh” |
| Kiểm chứng | `tests/Management.SmokeTests` | Chạy trên DB tạm; thêm ca cho hành vi mới, không xóa ca phân quyền/race chỉ để test xanh |

### Chuyển database cũ và dọn role đã bỏ

- Migration `TableFloorAndReservationLifecycle` thêm tầng/khu vực ưu tiên/thời điểm kết thúc; migration `MenuApprovalAndKitchenOwnership` chỉ thêm `DaDuyet` (món cũ mặc định đã duyệt) và `LyDoNgung`. Không xóa món, size, bill hoặc công thức cũ.
- Role/tài khoản mẫu đã bỏ không được tự nâng thành Admin/Bếp. Nhánh này không tạo lại chúng và không cho đăng nhập bằng role đã bỏ.
- Dọn database Development cũ bằng lệnh bên dưới **sau khi kiểm tra connection string và sao lưu nếu có dữ liệu cần giữ**. Lệnh kiểm tra tài khoản dùng chung, lịch bàn, hóa đơn, phiếu nhập/xuất; nếu có liên kết thì dừng toàn bộ, không xóa lịch sử. Khi đó Admin đổi phân công hồ sơ thay vì xóa.

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project RestaurantManagement.Web -- --remove-retired-menu-roles
```

- Lệnh chỉ dọn role/tài khoản/hồ sơ mẫu cũ không có lịch sử; chạy lại không tác động tài khoản khác. Không tự dọn lúc khởi động, không chạy ở Production. Database preview local đã được dọn theo yêu cầu ngày 02/10; Git không đồng bộ việc xóa sang database máy khác.

## 7. Kiểm chứng ngày 02/10/2026

- Build bộ kiểm tra và Web: **0 cảnh báo, 0 lỗi**.
- Kiểm thử tích hợp bản cuối: **PASS: 481 HTTP/database checks**, trên database tạm đã được xóa sau khi chạy. Có kiểm tra đúng/sai cổng và quyền của 7 role hiện tại; menu; khóa/đổi mật khẩu/thu hồi phiên; sở hữu lịch khách; gợi ý/xếp bàn đồng thời và kết thúc/dọn bàn; Bếp giả gửi giá/duyệt; món chờ duyệt không public; bill hủy không vào KDS; phiên bản thanh toán sai; dọn role cũ có/không có lịch sử. Số ca thay đổi theo bộ vai trò và hành vi hiện tại, không dùng số ca cũ làm tiến độ chức năng.
- Kiểm tra migration sơ đồ bàn trước đó (từ 2 lên 3 migrations) trên dữ liệu SQL của nhóm: giữ nguyên **18 bàn, 16 món, 8 đặt bàn, 4 hóa đơn** và checksum thông tin đặt bàn; khép đúng 4 lượt lịch sử đủ điều kiện. Đây không phải bằng chứng riêng về nâng toàn bộ schema từ 2 lên 4 migrations.
- Migration thứ 4 `MenuApprovalAndKitchenOwnership` thêm cột, đã áp dụng trên database kiểm thử/preview. Kiểm tra EF: **No changes have been made to the model since the last migration**. Kiểm tra cú pháp JS sơ đồ/form món và `git diff --check` đạt; không có thao tác xóa database ứng dụng.
- Đã bấm chuyển trang/menu trên trình duyệt desktop và khung nhỏ; menu thu gọn không che nội dung, chuyển trang đóng menu. Nhân viên xem trang công khai không có link đặt bàn của khách. Polling sơ đồ có giới hạn chờ; hai truy vấn danh sách/lịch sử đặt bàn dùng split query tránh nhân dòng khi tải các bảng con. Chưa kiểm thử tải lớn để kết luận mọi vấn đề hiệu năng đã hết.
- **Mốc lịch sử trước cập nhật 03/10:** thời điểm chạy 481 ca, UI Bồi bàn xác nhận món đã phục vụ còn bàn giao. Mốc 03/10 ngay dưới đã bổ sung bước này; hiện không còn thiếu `DaPhucVu`. Giữ ghi nhận lịch sử, nhưng khi nối tiếp phải dùng mục 5–6 và kiểm chứng 09/10, không lấy kết luận thiếu ở mốc cũ làm nhiệm vụ mới.

## 8. Kiểm chứng ngày 03/10/2026 — Thành viên 2 (Gọi món, Bếp & Thanh toán đa hình thức)

- Build toàn bộ solution (Shared, API, Web, Tests): **0 cảnh báo, 0 lỗi**.
- Kiểm thử tích hợp tự động: **PASS: 532 HTTP/database checks** (tăng thêm 51 ca kiểm thử mới từ 481 lên 532), chạy trên database tạm và tự động dọn sạch sau khi hoàn tất.
- Các nội dung kiểm thử đã xác minh:
  1. Ghi nhận gọi món mang đi (`MangDi`) và giao hàng (`GiaoHang`) có địa chỉ nhận hàng, lưu thông tin người nhận và tính tổng tiền chính xác.
  2. Ghi nhận gọi món tại bàn đang phục vụ (`TaiBan`) liên kết chuẩn xác với lượt khách đang phục vụ và hóa đơn hiện hành.
  3. Gọi thêm món vào đơn hàng đang phục vụ (`/DonHang/AddDishes`), lưu snapshot tên size `TenSizeLucBan` và thời điểm gọi `ThoiDiemGoi` riêng cho từng dòng order.
  4. Màn hình KDS Bếp `/Bep` hiển thị đúng size món ăn, thời gian gọi (sắp xếp FIFO), phân biệt rõ ràng đơn tại bàn/mang đi/giao hàng; chuyển trạng thái tuần tự `ChoCheBien` → `DangCheBien` → `SanSang`.
  5. Thao tác Bồi bàn / Quản trị xác nhận món đã mang ra bàn (`SanSang` → `DaPhucVu`), giải quyết điều kiện tiên quyết để giải phóng bàn trong `TableService`.
  6. Thao tác hủy món (`ChoCheBien` → `DaHuy`) khi khách đổi ý trước khi nấu, tự động điều chỉnh giảm tổng tiền hàng của hóa đơn tương ứng.
  7. Thanh toán đa hình thức:
     - Chuyển khoản ngân hàng VietQR: sinh mã QR chuẩn VietQR (MBBank 999988889999), lưu mã đối soát giao dịch `MaGiaoDich`.
     - Quẹt thẻ POS: lưu thông tin loại thẻ, 4 số cuối thẻ và mã chuẩn chi POS.
     - Tiền mặt: tính chính xác số tiền khách đưa, tiền thừa thối lại và chiết khấu.
  8. Xuất và in hóa đơn đẹp mắt, chuẩn hóa print CSS cho khổ giấy in nhiệt K80 và khổ A5.
  9. Sau khi phục vụ hết các món và thanh toán đủ, `TableService` cho phép bồi bàn bấm **Kết thúc phục vụ** (`DangPhucVu` → `CanDon`) và **Dọn xong** (`CanDon` → `SanSang`) trọn vẹn 100%.
- Migration thứ 5 `OrderFulfillmentAndMultiPayment` bổ sung các cột mới với giá trị mặc định và backfill dữ liệu lịch sử không mất mát.
