1. mình có yêu cầu sau cho chatgpt và chatgpt đã tìm được kết quả là repo này
"mình cần tìm một repo codebase cho một module nhỏ, có thể là github, gitlab, stackoverflow,... với yêu cầu sau là một module quản lý doanh thu: cơ bản chỉ cần 2 đối tượng chính là buyer và product, có thêm bill nếu có buyer phải có tính thuộc tính xác định ưu đãi mua nhiều, ví dụ như VIP, quy đổi giá trị lịch sử bill sang điểm và quy điểm điểm sang khuyến mãi product có giá để tính doanh thu có thể có bill để thực hiện tính doanh thu dễ hơn nếu khách hàng mua có ap voucher ưu đãu từ thuộc tính vip/ điểm tích lũy techstack: .net core, angular, posgrest đáp ứng 3 yêu cầu sau theo % bắt buộc phải đáp ứng - techstack: 100% - chức năng y hệ: 95% - đồ độc lập của module: 90% hãy tìm cho mình 5 cái phù hợp nhất có thể"
bạn hãy kiểm tra xem repo thế nào nhé

2.ok vậy thì khá ổn cho việc custome lại theo ý mình, có một vài nhận định cần bạn xác nhận cùng mình
- techstack : OK không bàn
- requi yêu cầu: chúng ta có thể chỉ cần thêm một thuộc tính VipPoint vào buyer, input quy đổi điểm sẽ đi qua middleware riêng: chúng ta sẽ chốt logic: cứ 1 triệu đồng giá trị đơn hàng-> 1 điểm vào VipPoint. output ra khuyến mãi cũng sẽ có middleware riêng, chỉ được sử dụng khi có tối tối thiểu 5 điểm VipPoint, mỗi điểm VipPoint sẽ được giảm 1% giá trị bill được áp điểm, giá trị giảm không được vượt quá 20% giá trị nhận điểm, tức là như trên ở bước quy đổi input 1 điểm = 1 triệu, nếu người dùng quyết định dùng 10 điểm, tức là giá trị điểm là 10 triệu, thì sau khi áp, giảm bill được app voucher tối đã chỉ là 20% của 10 triệu, tức 2 triệu, kể cả bill đó là 100 triệu và 10% là 10 triệu đi chăng nữa. về sản phẩm, có lẽ sẽ không cần voucher gắn theo sản phẩm, chỉ có voucher từ  VipPoint đi qua middleware từ buyer, nếu cần lưu lại thì sau khi sẽ được gán vào voucher. chúng ta sẽ làm phần quản lý doanh thu từ bill là ổn nhất.
ok đây là phần ý tưởng chuyển đổi và custome ban đầu từ repo codebase, xác nhận xong thì chúng ta sẽ đi sâu hơn vào thiết kế bản custome độc lập

3.
ok về phần chốt
1. 
“Giá trị đơn” để tích điểm là trước hay sau thuế/phí ship?: giá trị đơn quy đổi là tổng giá trị sản phẩm sau khi áp voucher cho bill đó nếu có (tức tổng bill 10 triệu, áp voucher giảm 2 triệu còn 8 thì tính 8 điểm). không tính thuế/ship/ giá trị gia tăng,...
Có trừ hàng hoàn / hủy không?: voucher/VipPoint sau khi áp vào đơn hàng sẽ không thể hoàn trả nếu buyer hủy đơn sau khi xác đặt hàng
Có làm tròn từng đơn ?: quy đổi đến hàng 1 sau phẩy, tức ví dụ giá trị đơn là 2 450 000 đồng thì quy đổi ra 2,4 điểm ( không làm tròn lên hay xuống nữa)
2. Thời điểm cộng VipPoint: khi tạo đơn, khi thanh toán xong, hay khi CompletedOrder? chỉ khi đơn đã CompletedOrder là chuẩn nhất (đúng chứ?)
3. Bill phần được áp VipPoint là tổng trước giảm hay đã trừ các khuyến mãi khác?: bill được giảm là giá trị cuối cùng của bill, sau khi cộng trừ từ mọi nguồn khác.
4. người dùng phải có ít nhất 5 điểm VipPoint và mỗi lần sử dụng phải dùng ít nhất 5 điểm VipPoint.
xác nhận xong sẽ đi sâu nhé

4.về câu hỏi Điểm cần làm rõ thêm (quan trọng): Khi tích điểm sau CompletedOrder, cơ sở 8 triệu trong ví dụ của bạn có đã trừ luôn phần giảm từ VipPoint của chính đơn đó chưa? của bạn: VipPoint được tính chính xác giá cuối cùng của bill, nói dễ hiểu là dựa trên toàn bộ số tiền buyer phải trả
Quy tắc truncate một chữ số thập phân (ví dụ công thức hoặc ví dụ biên 2,49 → 2,4)?: đúng kể cả là 4 499 000 thì vẫn là 4,4 VipPoint. các phần còn lại đã ok, xác nhận nốt 2 spec này rồi chúng ta sẽ đi sâu
