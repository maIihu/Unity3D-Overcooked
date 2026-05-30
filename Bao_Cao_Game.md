# BÁO CÁO ĐÁNH GIÁ DỰ ÁN GAME OVERCOOKED 3D MULTIPLAYER
**Giảng viên đánh giá:** Khoa Công nghệ Thông tin  
**Môn học:** Phát triển ứng dụng Game / Kỹ nghệ phần mềm ứng dụng  
**Tên báo cáo:** Bao_Cao_Game.md  

---

## LỜI MỞ ĐẦU
Chào em, thầy đã nhận được mã nguồn dự án game **Unity3D-Overcooked Multiplayer** của em. Sau khi đọc và phân tích toàn bộ cấu trúc dự án từ kiến trúc hệ thống, cách tổ chức thư mục, các mẫu thiết kế (Design Patterns) áp dụng cho đến giải pháp lập trình mạng, thầy đánh giá đây là một đồ án hoàn thiện rất tốt, có tính thực tiễn cao và áp dụng nhiều kỹ thuật lập trình nâng cao trong Unity. 

Dưới đây là tổng hợp chi tiết báo cáo kỹ thuật của dự án này, bao gồm: **Công nghệ sử dụng**, **Cách thức triển khai** và **Nhận xét chuyên môn từ Giảng viên**.

---

## 1. TỔNG QUAN DỰ ÁN
Dự án là một phiên bản tái hiện (clone) của tựa game nổi tiếng **Overcooked** dưới dạng đồ họa 3D, hỗ trợ cả hai chế độ chơi đơn (Singleplayer) và chơi mạng nhiều người (Multiplayer). 
* **Gameplay chính:** Người chơi điều khiển các đầu bếp di chuyển trong bếp, tương tác với các bàn làm việc (Counters) để lấy nguyên liệu, sơ chế (cắt thái), nấu nướng, trình bày lên đĩa và giao món ăn theo đơn đặt hàng trước khi hết thời gian.
* **Điểm nổi bật:** Dự án đã tích hợp thành công giải pháp đồng bộ mạng thời gian thực và xây dựng một hệ thống **Level Editor (Trình thiết kế màn chơi)** trực quan cho phép tạo và lưu màn chơi dưới dạng dữ liệu tuần tự hóa JSON.

---

## 2. CÔNG NGHỆ SỬ DỤNG (TECHNOLOGY STACK)

Dự án áp dụng bộ công nghệ tiêu chuẩn công nghiệp hiện nay đối với phát triển game multiplayer:

### A. Engine chính & Đồ họa
* **Unity Engine**: Sử dụng làm nền tảng cốt lõi quản lý Rendering (3D), Physics (tương tác vật lý va chạm của nhân vật), Scene Management (chuyển đổi giữa các phân cảnh MainMenu, Lobby, Gameplay), và UI Canvas.

### B. Giải pháp lập trình mạng (Multiplayer Networking)
* **Photon Fusion**: Lựa chọn giải pháp Netcode tiên tiến hàng đầu hiện nay cho Unity thay vì Mirror hay Netcode for GameObjects cũ.
  * **Mô hình đồng bộ:** State Synchronization (Đồng bộ trạng thái) kết hợp Client-Side Prediction (Dự đoán từ phía Client) giúp giảm thiểu độ trễ.
  * **Cơ chế Host-Migration & Client/Server:** Quản lý quyền kiểm soát trạng thái game thông qua `HasStateAuthority` trên Server/Host.

### C. Tiện ích & Thư viện bổ trợ (Third-party Libraries)
* **DOTween (Demigiant)**: Sử dụng để xử lý animation cho giao diện (UI fade in/out), hiệu ứng nhấp nháy cảnh báo (Warning UI) khi món ăn sắp cháy trên bếp lò mà không cần tạo Animator Controller cồng kềnh, tối ưu hiệu năng CPU.
* **Unity ObjectPool API (`UnityEngine.Pool`)**: Thư viện chuẩn của Unity được bọc (wrapper) lại giúp tối ưu hóa việc tái sử dụng GameObject.

---

## 3. KIẾN TRÚC HỆ THỐNG & CÁC DESIGN PATTERNS ÁP DỤNG

Dự án thể hiện tư duy thiết kế phần mềm rất mạch lạc thông qua việc áp dụng nhuần nhuyễn các mẫu thiết kế kinh điển:

### A. Singleton Pattern
* **Mục đích:** Đảm bảo chỉ có một thực thể duy nhất quản lý các phân hệ toàn cục và dễ dàng truy cập từ bất kỳ đâu.
* **Triển khai:** Khai báo lớp abstract generic [Singleton.cs](file:///d:/GameProject/Unity3D-Overcooked/Assets/_Game/Scripts/DesignPattern/Singleton.cs) kế thừa từ `MonoBehaviour`.
* **Áp dụng:** 
  * `MessageManager`: Quản lý truyền nhận sự kiện toàn game.
  * `PoolManager`: Quản lý tất cả các Pool chứa đối tượng.
  * `FusionNetworkRunner`: Quản lý kết nối và trạng thái phòng chơi.
  * `UIManager`: Điều phối các màn hình giao diện.

### B. Observer Pattern (Mẫu thiết kế Quan sát)
* **Mục đích:** Giảm thiểu sự phụ thuộc trực tiếp (tight coupling) giữa các thành phần logic như Game Timer, Score Controller và hệ thống UI.
* **Triển khai:** Lớp [MessageManager.cs](file:///d:/GameProject/Unity3D-Overcooked/Assets/_Game/Scripts/DesignPattern/Observer/MessageManager.cs) triển khai cơ chế đăng ký và phát sự kiện thông qua:
  * Lớp `Message` chứa thông tin sự kiện (`ProjectMessageType`) cùng dữ liệu kèm theo (`object[] Data`).
  * Giao diện `IMessageHandle` quy định phương thức nhận tin `Handle(Message message)`.
  * Các loại Event chính: `OnLoadLevel`, `OnSpawnNewRecipe`, `OnRecipeSuccess`, `OnScoreChanged`, `OnTimerTick`, `OnGameOver`.

### C. Object Pooling Pattern (Mẫu thiết kế Tối ưu bộ nhớ)
* **Mục đích:** Tránh việc gọi liên tục `Instantiate()` và `Destroy()` cho các đối tượng sinh/hủy thường xuyên như nguyên liệu, món ăn, đĩa, gây phân mảnh bộ nhớ và giật lag (GC Alloc spikes).
* **Triển khai:** Hệ thống nằm trong thư mục [Pooling](file:///d:/GameProject/Unity3D-Overcooked/Assets/_Game/Scripts/Pooling) gồm:
  * `PoolManager`: Hub trung tâm điều phối.
  * `KitchenObjectPool` & `CounterPool`: Quản lý chuyên biệt các loại bếp lò, thớt và thực phẩm.
  * Giao diện `IPoolable` định nghĩa 2 trạng thái `OnSpawn()` và `OnDespawn()` để khởi tạo/dọn dẹp tài nguyên khi tái sử dụng.

### D. Finite State Machine - FSM (Máy trạng thái hữu hạn)
* **Mục đích:** Quản lý quy trình nấu nướng phức tạp tại bếp lò (Stove).
* **Triển khai:** Lớp [StoveCounter.cs](file:///d:/GameProject/Unity3D-Overcooked/Assets/_Game/Scripts/Counter/StoveCounter.cs) quản lý trạng thái lò qua Enum `StoveState`:
  * Trạng thái: `Idle` (Trống/Chưa nấu) $\rightarrow$ `Frying` (Đang rán/nấu) $\rightarrow$ `Fried` (Đã chín) $\rightarrow$ `Burned` (Bị cháy khét).
  * Host kiểm soát quá trình cập nhật trạng thái thông qua Fusion tick (`FixedUpdateNetwork`) dựa trên lượng thời gian `FryingTimer` và `BurningTimer`.

---

## 4. TRIỂN KHAI CHI TIẾT CÁC HỆ THỐNG CHÍNH

### A. Hệ thống Tương tác Gameplay (Counters & Kitchen Objects)
* **Lớp cơ sở [BaseCounter.cs](file:///d:/GameProject/Unity3D-Overcooked/Assets/_Game/Scripts/Counter/BaseCounter.cs)**: Định nghĩa các thuộc tính cơ bản như điểm đặt đồ vật (`counterTopPoint`), các phương thức tương tác ảo `Interact(Player player)` và `InteractAlternate(Player player)` (như ấn giữ để thái thực phẩm).
* **Các bàn bếp chuyên biệt kế thừa `BaseCounter`:**
  * `ClearCounter`: Bàn trống để đồ trung chuyển.
  * `ContainerCounter`: Hộp chứa nguyên liệu (lấy cà chua, hành, thịt sống...).
  * `CuttingCounter`: Bàn thớt thái thực phẩm, yêu cầu tương tác nhấp nút liên tục.
  * `StoveCounter`: Bếp nấu/rán thực phẩm (sử dụng nồi `PotObject` hoặc chảo `PanObject`).
  * `PlatesCounter`: Bàn chứa đĩa sạch.
  * `DeliveryCounter`: Cửa trả món ăn để ghi điểm.
  * `SinkCounter`: Bồn rửa đĩa bẩn.

### B. Cơ chế Đồng bộ Mạng (Photon Fusion Integration)
* **Đồng bộ hóa Trạng thái:** Các biến quan trọng như vị trí nấu, trạng thái nấu nướng, dữ liệu đầu vào người chơi được khai báo bằng thuộc tính `[Networked]` để Photon Fusion tự động truyền dữ liệu từ Host về các Client.
* **Cơ chế Input Handler:** Lớp `NetworkInputHandler` thu thập thông tin phím bấm của Client cục bộ và đóng gói vào struct `NetworkInputData`, gửi lên Server thông qua callback `OnInput` của `INetworkRunnerCallbacks`.
* **Chuyển đổi Dữ liệu từ Lobby sang Game:**
  1. Ở Lobby, người chơi chọn màu sắc ưa thích, thông tin được lưu trên đối tượng mạng `LobbyPlayer`.
  2. Khi chuyển Scene sang `GameScene` (bắt đầu chơi), Host đọc dữ liệu màu từ `LobbyPlayer`, sinh ra (Spawn) prefab `Player` tương ứng và gán mã màu phù hợp rồi mới tiến hành hủy `LobbyPlayer` cũ.

### C. Hệ thống Trình biên tập Màn chơi (Level Editor)
Đây là một tính năng mở rộng cực kỳ chất lượng của dự án:
* **Quản lý:** Lớp [LevelDesignerManager.cs](file:///d:/GameProject/Unity3D-Overcooked/Assets/_Game/Scripts/LevelEditor/LevelDesignerManager.cs) điều hành toàn bộ quá trình thiết kế.
* **Dữ liệu màn chơi (`LevelData`):** Lưu trữ danh sách các Counter cùng tọa độ (`Vector3 position`), góc quay (`rotation`) và loại nguyên liệu đặt sẵn trên đó.
* **Tuần tự hóa (Serialization):**
  * Dữ liệu được mã hóa sang chuỗi JSON bằng `JsonUtility.ToJson()`.
  * Ghi trực tiếp xuống file text đặt tại thư mục `Assets/Resources/Levels/Level_[Tên_màn].json`.
  * Khi chơi, hệ thống sử dụng `Resources.Load` đọc file JSON và tái tạo màn chơi động bằng cách duyệt danh sách và `Instantiate` các prefab Counter tại vị trí định sẵn.

---

## 5. ĐÁNH GIÁ VÀ NHẬN XÉT CỦA GIẢNG VIÊN

### Điểm mạnh (Strengths)
1. **Kiến trúc phần mềm chuẩn mực:** Áp dụng OOP tốt, phân chia module rõ ràng (mỗi Counter đảm nhận một nhiệm vụ chuyên biệt). Hệ thống Event Observer giúp code không bị rối và dễ mở rộng.
2. **Khai thác tốt Photon Fusion:** Xử lý vòng đời mạng rất chính xác thông qua việc tách biệt logic chạy trên Host (`FixedUpdateNetwork`) và visual chạy trên Client (`Render`), tuân thủ đúng chuẩn Server-Authoritative.
3. **Quản lý tài nguyên thông minh:** Sử dụng Object Pooling cho các đối tượng sinh sản liên tục giúp game chạy mượt mà trên các thiết bị cấu hình yếu.
4. **Tính năng Level Editor sáng tạo:** Cho thấy khả năng lập trình hệ thống tốt, biết ứng dụng JSON Serialization để lưu trữ cấu hình trò chơi.

### Điểm cần cải thiện (Areas for Improvement)
1. **Dọn dẹp code rác:** Vẫn còn một số đoạn code cũ bị comment lại (ví dụ trong [MessageManager.cs](file:///d:/GameProject/Unity3D-Overcooked/Assets/_Game/Scripts/DesignPattern/Observer/MessageManager.cs)). Em nên xóa sạch các dòng code thừa này trước khi đóng gói sản phẩm để đảm bảo tính thẩm mỹ của mã nguồn.
2. **Xử lý mất kết nối (Network Exception Handling):** Hiện tại hệ thống chưa có phần bắt lỗi chi tiết hoặc cơ chế Reconnect tự động khi Client bị rớt mạng đột ngột.
3. **Cơ chế hóa ScriptableObjects:** Thư mục `ScriptableObjects` hiện tại đang trống, một số dữ liệu cấu hình như thời gian nấu hay công thức nấu ăn nên được chuyển hẳn thành các file Asset `.asset` (ScriptableObject) thay vì hardcode giá trị float/int trong code để Game Designer dễ dàng cân bằng game.

---
**ĐÁNH GIÁ CHUNG (GRADE):** **A (Xuất sắc)**  
*Đồ án đạt yêu cầu kỹ thuật rất cao, cấu trúc lập trình chuyên nghiệp. Chúc mừng em đã hoàn thành xuất sắc dự án này!*
