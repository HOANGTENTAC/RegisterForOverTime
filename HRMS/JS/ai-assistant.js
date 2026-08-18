// ===============================
// HR AI ASSISTANT
// ===============================

document.addEventListener("DOMContentLoaded", function () {

    const panel = document.getElementById("aiPanel");
    const toggle = document.getElementById("aiToggle");
    const close = document.getElementById("btnCloseAI");

    const sendBtn = document.getElementById("btnSend");
    const input = document.getElementById("txtMessage");

    const body = document.getElementById("chatBody");
    const typing = document.getElementById("typing");

    //==============================
    // OPEN / CLOSE
    //==============================

    toggle.addEventListener("click", function () {

        panel.classList.toggle("show");

        if (panel.classList.contains("show")) {

            setTimeout(() => {

                input.focus();

            }, 300);

        }

    });

    close.addEventListener("click", function () {

        panel.classList.remove("show");

    });

    //==============================
    // QUICK BUTTON
    //==============================

    document.querySelectorAll(".quick-btn").forEach(function (btn) {

        btn.addEventListener("click", function () {

            input.value = btn.innerText;

            sendMessage();

        });

    });

    //==============================
    // ENTER
    //==============================

    input.addEventListener("keydown", function (e) {

        if (e.key === "Enter" && !e.shiftKey) {

            e.preventDefault();

            sendMessage();

        }

    });

    sendBtn.addEventListener("click", sendMessage);

    //==============================
    // AUTO HEIGHT
    //==============================

    input.addEventListener("input", function () {

        this.style.height = "42px";

        this.style.height = this.scrollHeight + "px";

    });

    //==============================
    // SEND MESSAGE
    //==============================

    function sendMessage() {

        let text = input.value.trim();

        if (text == "") return;

        appendUser(text);

        input.value = "";

        input.style.height = "42px";

        typing.style.display = "flex";

        scrollBottom();

        //==========================
        // Demo
        // Sau này thay bằng fetch()
        //==========================

        setTimeout(function () {

            typing.style.display = "none";

            appendBot(getDemoAnswer(text));

        }, 1200);

    }

    //==============================
    // USER MESSAGE
    //==============================

    function appendUser(text) {

        body.insertAdjacentHTML(

            "beforeend",

            `
            <div class="message user">
                <div class="bubble">${escapeHtml(text)}</div>
            </div>
            `
        );

        scrollBottom();

    }

    //==============================
    // BOT MESSAGE
    //==============================

    function appendBot(text) {

        body.insertAdjacentHTML(

            "beforeend",

            `
            <div class="message bot">
                <div class="bubble">${text}</div>
            </div>
            `
        );

        scrollBottom();

    }

    //==============================
    // DEMO
    //==============================

    function getDemoAnswer(q) {

        q = q.toLowerCase();

        if (q.includes("nghỉ")) {

            return `
                📄 <b>Đăng ký nghỉ phép</b><br><br>
                Bạn có thể tạo phiếu nghỉ phép ngay.<br><br>
                <button class="quick-btn">
                    Tạo phiếu nghỉ
                </button>
            `;

        }

        if (q.includes("lương")) {

            return `
                💰 <b>Bảng lương</b><br><br>
                Tôi có thể tra cứu bảng lương của bạn.<br><br>
                (Sau này sẽ lấy dữ liệu từ Database)
            `;

        }

        if (q.includes("tăng ca") || q.includes("ot")) {
            return `
            ⏰ <b>Đăng ký tăng ca</b><br><br>
            Các bước cần thực hiện:<br>
            1️⃣ Chọn ngày muốn OT<br>
            2️⃣ Chọn khung giờ bắt đầu/kết thúc<br>
            3️⃣ Nhập lý do tăng ca<br>
            4️⃣ Gửi phiếu để quản lý phê duyệt<br>
            5️⃣ Chọn người đăng ký tăng ca<br><br>
            <button class="quick-btn" onclick="redirectOT()">
                Đi đến trang đăng ký
            </button>
            `;
        }


        if (q.includes("chấm công")) {

            return `
                📅 Tôi có thể kiểm tra lịch chấm công cho bạn.
            `;

        }

        return `
            Xin lỗi 😄<br><br>
            Tôi chưa hiểu câu hỏi của bạn.<br><br>
            Khi kết nối AI, tôi sẽ trả lời thông minh hơn.
        `;

    }

    //==============================
    // SCROLL
    //==============================

    function scrollBottom() {

        body.scrollTop = body.scrollHeight;

    }

    //==============================
    // HTML SAFE
    //==============================

    function escapeHtml(text) {

        return text
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;");

    }

});


//==============================
// REDIRECT
//==============================
function redirectOT() {
    window.location.href = "/OverTime/Index";
}