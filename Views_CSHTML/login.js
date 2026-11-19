document.addEventListener("DOMContentLoaded", () => {
    const password = document.getElementById("password");
    const togglePassword = document.getElementById("togglePassword");

    const loginTab = document.getElementById("loginTab");
    const registerTab = document.getElementById("registerTab");

    const formTitle = document.getElementById("formTitle");
    const formDescription = document.getElementById("formDescription");
    const submitBtn = document.getElementById("submitBtn");
    const footerText = document.getElementById("footerText");
    const footerSwitch = document.getElementById("footerSwitch");
    const loginForm = document.getElementById("loginForm");

    let currentMode = "login";

    // Hiện / ẩn mật khẩu
    togglePassword.addEventListener("click", () => {
        const isPassword = password.type === "password";

        password.type = isPassword ? "text" : "password";
        togglePassword.textContent = isPassword ? "⊗" : "◉";
        togglePassword.setAttribute(
            "aria-label",
            isPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"
        );
    });

    // Chuyển chế độ đăng nhập / đăng ký
    function switchMode(mode) {
        currentMode = mode;

        const isLogin = mode === "login";

        loginTab.classList.toggle("active", isLogin);
        registerTab.classList.toggle("active", !isLogin);

        formTitle.textContent = isLogin
            ? "Chào mừng trở lại!"
            : "Tạo tài khoản mới!";

        formDescription.textContent = isLogin
            ? "Đăng nhập để tiếp tục hành trình học tiếng Trung của bạn."
            : "Đăng ký để bắt đầu hành trình chinh phục tiếng Trung.";

        submitBtn.innerHTML = isLogin
            ? "<span>➜</span> Đăng nhập"
            : "<span>➜</span> Tạo tài khoản";

        footerText.textContent = isLogin
            ? "Chưa có tài khoản?"
            : "Đã có tài khoản?";

        footerSwitch.textContent = isLogin
            ? "Đăng ký ngay >"
            : "Đăng nhập ngay >";

        password.autocomplete = isLogin
            ? "current-password"
            : "new-password";

        password.placeholder = isLogin
            ? "Mật khẩu"
            : "Tạo mật khẩu";

        const quickLogin = document.querySelector(".quick-login");
        const divider = document.querySelector(".divider");
        const socialLogin = document.querySelector(".social-login");

        quickLogin.hidden = !isLogin;
        divider.hidden = !isLogin;
        socialLogin.hidden = !isLogin;

        // Thêm trường xác nhận mật khẩu khi đăng ký
        let confirmGroup = document.getElementById("confirmGroup");

        if (!isLogin && !confirmGroup) {
            confirmGroup = document.createElement("div");
            confirmGroup.className = "input-group";
            confirmGroup.id = "confirmGroup";

            confirmGroup.innerHTML = `
                <span class="input-icon">♙</span>
                <input
                    type="password"
                    id="confirmPassword"
                    placeholder="Nhập lại mật khẩu"
                    autocomplete="new-password"
                    required
                >
            `;

            password.parentElement.after(confirmGroup);
        }

        if (isLogin && confirmGroup) {
            confirmGroup.remove();
        }
    }

    loginTab.addEventListener("click", () => {
        switchMode("login");
    });

    registerTab.addEventListener("click", () => {
        switchMode("register");
    });

    footerSwitch.addEventListener("click", () => {
        switchMode(currentMode === "login" ? "register" : "login");
    });

    // Kiểm tra form
    loginForm.addEventListener("submit", async (event) => {
        event.preventDefault();

        const email = document.getElementById("email").value.trim();
        const passwordValue = password.value;

        if (!email || !passwordValue) {
            alert("Vui lòng nhập đầy đủ thông tin!");
            return;
        }

        if (currentMode === "register") {
            const confirmPassword =
                document.getElementById("confirmPassword").value;

            if (passwordValue.length < 8) {
                alert("Mật khẩu cần có ít nhất 8 ký tự!");
                return;
            }

            if (passwordValue !== confirmPassword) {
                alert("Mật khẩu xác nhận không khớp!");
                return;
            }

            alert("Thông tin hợp lệ. Hãy kết nối API đăng ký để hoàn tất.");
            return;
        }

        /*
         * TÍCH HỢP API ĐĂNG NHẬP HIỆN TẠI TẠI ĐÂY.
         * Không thay đổi endpoint hoặc cơ chế xác thực của ứng dụng.
         */

        alert("Giao diện đã hoạt động. Cần kết nối API để đăng nhập thực tế.");
    });

    // Quên mật khẩu
    document.getElementById("forgotPassword")
        .addEventListener("click", (event) => {
            event.preventDefault();
            alert("Vui lòng kết nối chức năng khôi phục mật khẩu.");
        });

    // Tài khoản mẫu
    document.querySelector(".quick-login")
        .addEventListener("click", () => {
            alert("Hãy tích hợp chức năng đăng nhập tài khoản mẫu tại đây.");
        });
});
