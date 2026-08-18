$(function () {

    const notificationUrl =
        window.notificationUrls &&
        window.notificationUrls.passwordResetSummary;

    if (!notificationUrl) {
        return;
    }

    const $badge = $("#passwordResetBadge");
    const $content = $("#notificationContent");

    if (!$badge.length || !$content.length) {
        return;
    }

    // =========================================================
    // INIT
    // =========================================================

    loadNotifications();

    // =========================================================
    // PUBLIC REFRESH
    // =========================================================

    window.refreshNotifications = function () {
        loadNotifications();
    };

    // =========================================================
    // LOAD NOTIFICATIONS
    // =========================================================

    function loadNotifications() {

        $.ajax({
            url: notificationUrl,
            type: "GET",
            cache: false,

            success: function (res) {

                if (!res || !res.success) {
                    renderEmpty();
                    return;
                }

                const count = parseInt(res.count, 10) || 0;

                if (count <= 0) {
                    renderEmpty();
                    return;
                }

                renderPasswordResetNotification(count, res.url);
            },

            error: function () {
                // Không nên tự xóa notification
                // chỉ vì API tạm thời lỗi
                console.error("Không thể tải thông báo.");
            }
        });
    }

    // =========================================================
    // PASSWORD RESET NOTIFICATION
    // =========================================================

    function renderPasswordResetNotification(count, url) {

        $badge
            .removeClass("d-none")
            .text(count > 99 ? "99+" : count);

        const safeUrl = url || "#";

        $content.html(`
            <a href="${safeUrl}" class="notification-item">

                <div class="notification-item-icon">
                    <i class="fa-solid fa-key"></i>
                </div>

                <div class="notification-item-content">

                    <div class="notification-item-title">
                        Yêu cầu cấp lại mật khẩu
                    </div>

                    <div class="notification-item-description">
                        Có <strong>${count}</strong> tài khoản đang chờ xử lý
                    </div>

                    <div class="notification-item-action">
                        Xem danh sách
                        <i class="fa-solid fa-arrow-right"></i>
                    </div>

                </div>

            </a>
        `);
    }

    // =========================================================
    // EMPTY STATE
    // =========================================================

    function renderEmpty() {

        $badge
            .addClass("d-none")
            .text("0");

        $content.html(`
            <div class="notification-empty">

                <div class="notification-empty-icon">
                    <i class="fa-regular fa-bell-slash"></i>
                </div>

                <div class="notification-empty-title">
                    Không có thông báo mới
                </div>

                <div class="notification-empty-text">
                    Bạn đã xử lý hết các thông báo.
                </div>

            </div>
        `);
    }

});