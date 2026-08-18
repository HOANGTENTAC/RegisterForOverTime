$(function () {
    const $form = $("#accountFilterForm");

    if ($form.length === 0) {
        return;
    }

    const $keyword = $("#accountKeyword");
    const $dept = $("#accountDept");
    const $accessLevel = $("#accountAccessLevel");
    const $status = $("#accountStatus");

    const $tbody = $("#accountTableBody");

    const $kpiTotalAccounts = $("#kpiTotalAccounts");
    const $kpiTotalAdmins = $("#kpiTotalAdmins");
    const $kpiTotalManagers = $("#kpiTotalManagers");
    const $kpiTotalNoAccount = $("#kpiTotalNoAccount");

    const $tableCard = $(".accounts-table-card");
    const $searchButton = $(".btn-account-search");

    const roleModalEl = document.getElementById("roleModal");
    const resetPasswordModalEl = document.getElementById("resetPasswordModal");
    const createAccountModalEl = document.getElementById("createAccountModal");

    const roleModal = roleModalEl ? new bootstrap.Modal(roleModalEl) : null;
    const resetPasswordModal = resetPasswordModalEl ? new bootstrap.Modal(resetPasswordModalEl) : null;
    const createAccountModal = createAccountModalEl ? new bootstrap.Modal(createAccountModalEl) : null;

    const $roleEmployeeCd = $("#roleEmployeeCd");
    const $roleModalEmployeeText = $("#roleModalEmployeeText");
    const $roleDept = $("#roleDept");
    const $roleAccessLevel = $("#roleAccessLevel");
    const $roleTableBody = $("#roleTableBody");

    const $resetEmployeeCd = $("#resetEmployeeCd");
    const $resetPasswordEmployeeText = $("#resetPasswordEmployeeText");
    const $resetNewPassword = $("#resetNewPassword");

    const $createEmployeeCd = $("#createEmployeeCd");
    const $createAccountEmployeeText = $("#createAccountEmployeeText");
    const $createPassword = $("#createPassword");

    let searchTimer = null;
    let currentRequest = null;

    /* =========================================================
       FILTER EVENTS
    ========================================================= */

    $form.on("submit", function (e) {
        e.preventDefault();
        loadAccounts();
    });

    $dept.on("change", function () {
        loadAccounts();
    });

    $accessLevel.on("change", function () {
        loadAccounts();
    });

    $status.on("change", function () {
        loadAccounts();
    });

    $keyword.on("input", function () {
        clearTimeout(searchTimer);

        searchTimer = setTimeout(function () {
            loadAccounts();
        }, 350);
    });

    /* =========================================================
       TABLE ACTION EVENTS
    ========================================================= */

    $(document).on("click", ".btn-manage-role", function () {
        const employeeCd = $(this).data("employee-cd");
        const employeeName = $(this).data("employee-name");

        openRoleModal(employeeCd, employeeName);
    });

    $(document).on("click", ".btn-reset-password", function () {
        const employeeCd = $(this).data("employee-cd");
        const employeeName = $(this).data("employee-name");

        openResetPasswordModal(employeeCd, employeeName);
    });

    $(document).on("click", ".btn-create-account", function () {
        const employeeCd = $(this).data("employee-cd");
        const employeeName = $(this).data("employee-name");

        openCreateAccountModal(employeeCd, employeeName);
    });

    $(document).on("click", ".btn-delete-role", function () {
        const employeeCd = $(this).data("employee-cd");
        const dept = $(this).data("dept");

        deleteRole(employeeCd, dept);
    });

    $("#btnSaveRole").on("click", function () {
        saveRole();
    });

    $("#btnResetPassword").on("click", function () {
        resetPassword();
    });

    $("#btnCreateAccount").on("click", function () {
        createAccount();
    });

    $("#btnUseDefaultPassword").on("click", function () {
        $resetNewPassword.val("Tent@c");
    });

    $("#btnToggleResetPassword").on("click", function () {
        togglePassword($resetNewPassword, $(this));
    });

    $("#btnToggleCreatePassword").on("click", function () {
        togglePassword($createPassword, $(this));
    });

    /* =========================================================
       LOAD ACCOUNTS
    ========================================================= */

    function loadAccounts() {
        const params = {
            keyword: $.trim($keyword.val()),
            dept: $dept.val(),
            accessLevel: $accessLevel.val(),
            status: $status.val()
        };

        if (currentRequest) {
            currentRequest.abort();
        }

        setLoading(true);

        currentRequest = $.ajax({
            url: window.accountUrls.data,
            type: "GET",
            data: params,
            cache: false,

            success: function (res) {
                if (!res || !res.success) {
                    notifyError(res && res.message ? res.message : "Không tải được danh sách tài khoản");
                    renderNoData();
                    return;
                }

                updateSummary(res.summary);
                renderAccountTable(res.rows || []);
            },

            error: function (xhr, status) {
                if (status === "abort") {
                    return;
                }

                notifyError("Có lỗi xảy ra khi tải danh sách tài khoản");
                renderNoData();
            },

            complete: function () {
                setLoading(false);
                currentRequest = null;
            }
        });
    }

    function updateSummary(summary) {
        summary = summary || {
            totalAccounts: 0,
            totalAdmins: 0,
            totalManagers: 0,
            totalNoAccount: 0
        };

        $kpiTotalAccounts.text(summary.totalAccounts || 0);
        $kpiTotalAdmins.text(summary.totalAdmins || 0);
        $kpiTotalManagers.text(summary.totalManagers || 0);
        $kpiTotalNoAccount.text(summary.totalNoAccount || 0);
    }

    function renderAccountTable(rows) {
        if (!rows || rows.length === 0) {
            renderNoData();
            return;
        }

        let html = "";

        rows.forEach(function (user) {
            const employeeCd = user.EmployeeCD || "";
            const employeeName = user.TenNhanVien || "";

            const hasAccount = user.HasAccount === true;
            const roleLevel = user.HighestAccessLevel || "";
            const roleName = user.HighestAccessLevelName || "Chưa phân quyền";
            const managedDepartmentsText = user.ManagedDepartmentsText || "";
            const managedDepartmentsCount = user.ManagedDepartmentsCount || 0;
            const updatedDate = user.NgayCapNhat || "";

            html += `
                <tr>
                    <td class="fw-bold">${escapeHtml(employeeCd)}</td>

                    <td>
                        <div class="account-user">
                            <strong>${escapeHtml(employeeName)}</strong>
                            <span>${escapeHtml(employeeCd)}</span>
                        </div>
                    </td>

                    <td>${escapeHtml(user.TenPhongBan || "")}</td>

                    <td>
                        <span class="account-role-badge" data-level="${escapeHtml(roleLevel)}">
                            ${escapeHtml(roleName)}
                        </span>
                    </td>

                    <td>
                        ${managedDepartmentsText
                    ? `<span>${escapeHtml(managedDepartmentsText)}</span>`
                    : `<span class="text-muted">Chưa phân quyền</span>`
                }
                    </td>

                    <td class="text-center fw-bold">
                        ${escapeHtml(managedDepartmentsCount)}
                    </td>

                    <td>
                        ${hasAccount
                    ? `<span class="account-status status-active">Đã tạo tài khoản</span>`
                    : `<span class="account-status status-missing">Chưa có tài khoản</span>`
                }
                    </td>

                    <td>${escapeHtml(updatedDate)}</td>

                    <td>
                        <div class="d-flex gap-2 justify-content-center">
                            ${!hasAccount
                    ? `
                                        <button type="button"
                                                class="btn btn-sm btn-success btn-create-account"
                                                data-employee-cd="${escapeAttr(employeeCd)}"
                                                data-employee-name="${escapeAttr(employeeName)}">
                                            <i class="fa-solid fa-user-plus me-1"></i>
                                            Tạo
                                        </button>
                                    `
                    : ""
                }

                            <button type="button"
                                    class="btn btn-sm btn-outline-primary btn-manage-role"
                                    data-employee-cd="${escapeAttr(employeeCd)}"
                                    data-employee-name="${escapeAttr(employeeName)}">
                                <i class="fa-solid fa-user-gear me-1"></i>
                                Quyền
                            </button>

                            <button type="button"
                                    class="btn btn-sm btn-outline-warning btn-reset-password"
                                    data-employee-cd="${escapeAttr(employeeCd)}"
                                    data-employee-name="${escapeAttr(employeeName)}">
                                <i class="fa-solid fa-key me-1"></i>
                                Reset
                            </button>
                        </div>
                    </td>
                </tr>
            `;
        });

        $tbody.html(html);
    }

    function renderNoData() {
        $tbody.html(`
            <tr>
                <td colspan="9" class="text-center py-4 text-muted">
                    Không có dữ liệu tài khoản
                </td>
            </tr>
        `);

        updateSummary(null);
    }

    function setLoading(isLoading) {
        if (isLoading) {
            $tableCard.addClass("is-loading");

            $searchButton
                .prop("disabled", true)
                .html(`<span class="spinner-border spinner-border-sm"></span>`);

            return;
        }

        $tableCard.removeClass("is-loading");

        $searchButton
            .prop("disabled", false)
            .html(`<i class="fa-solid fa-search"></i>`);
    }

    /* =========================================================
       ROLE MODAL
    ========================================================= */

    function openRoleModal(employeeCd, employeeName) {
        $roleEmployeeCd.val(employeeCd);
        $roleModalEmployeeText.text(`${employeeCd} - ${employeeName}`);

        $roleDept.val("");
        $roleAccessLevel.val("");

        renderRoleLoading();

        if (roleModal) {
            roleModal.show();
        }

        loadRoles(employeeCd);
    }

    function loadRoles(employeeCd) {
        $.ajax({
            url: window.accountUrls.getRoles,
            type: "GET",
            data: {
                employeeCd: employeeCd
            },
            cache: false,

            success: function (res) {
                if (!res || !res.success) {
                    notifyError(res && res.message ? res.message : "Không tải được danh sách quyền");
                    renderRoleNoData();
                    return;
                }

                renderRoleTable(res.rows || []);
            },

            error: function () {
                notifyError("Có lỗi xảy ra khi tải danh sách quyền");
                renderRoleNoData();
            }
        });
    }

    function renderRoleLoading() {
        $roleTableBody.html(`
            <tr>
                <td colspan="4" class="text-center py-4 text-muted">
                    Đang tải dữ liệu phân quyền...
                </td>
            </tr>
        `);
    }

    function renderRoleNoData() {
        $roleTableBody.html(`
            <tr>
                <td colspan="4" class="text-center py-4 text-muted">
                    Chưa có phân quyền
                </td>
            </tr>
        `);
    }

    function renderRoleTable(rows) {
        if (!rows || rows.length === 0) {
            renderRoleNoData();
            return;
        }

        let html = "";

        rows.forEach(function (role) {
            html += `
                <tr>
                    <td>${escapeHtml(role.TenBoPhanQuanLy || role.BoPhanQuanLy || "")}</td>

                    <td class="text-center">
                        <span class="account-role-badge" data-level="${escapeAttr(role.AccessLevel || "")}">
                            ${escapeHtml(role.AccessLevelName || "")}
                        </span>
                    </td>

                    <td class="text-center">${escapeHtml(role.NgayCapNhat || "")}</td>

                    <td class="text-center">
                        <button type="button"
                                class="btn btn-sm btn-outline-danger btn-delete-role"
                                data-employee-cd="${escapeAttr(role.EmployeeCD || "")}"
                                data-dept="${escapeAttr(role.BoPhanQuanLy || "")}">
                            <i class="fa-solid fa-trash me-1"></i>
                            Xóa
                        </button>
                    </td>
                </tr>
            `;
        });

        $roleTableBody.html(html);
    }

    function saveRole() {
        const employeeCd = $roleEmployeeCd.val();
        const dept = $roleDept.val();
        const accessLevel = $roleAccessLevel.val();

        if (!employeeCd) {
            notifyWarning("Thiếu mã nhân viên");
            return;
        }

        if (!dept) {
            notifyWarning("Vui lòng chọn bộ phận quản lý");
            return;
        }

        if (!accessLevel) {
            notifyWarning("Vui lòng chọn cấp quyền");
            return;
        }

        const $button = $("#btnSaveRole");

        setButtonLoading($button, "Đang lưu...");

        $.ajax({
            url: window.accountUrls.saveRole,
            type: "POST",
            data: {
                EmployeeCD: employeeCd,
                BoPhanQuanLy: dept,
                AccessLevel: accessLevel
            },

            success: function (res) {
                if (!res || !res.success) {
                    notifyError(res && res.message ? res.message : "Không lưu được phân quyền");
                    return;
                }

                notifySuccess(res.message || "Đã lưu phân quyền thành công");

                $roleDept.val("");
                $roleAccessLevel.val("");

                loadRoles(employeeCd);
                loadAccounts();
            },

            error: function () {
                notifyError("Có lỗi xảy ra khi lưu phân quyền");
            },

            complete: function () {
                resetButton($button, `<i class="fa-solid fa-floppy-disk me-1"></i> Lưu phân quyền`);
            }
        });
    }

    function deleteRole(employeeCd, dept) {
        if (!employeeCd || !dept) {
            notifyWarning("Thiếu thông tin quyền cần xóa");
            return;
        }

        if (!confirm("Bạn có chắc muốn xóa quyền này không?")) {
            return;
        }

        $.ajax({
            url: window.accountUrls.deleteRole,
            type: "POST",
            data: {
                employeeCd: employeeCd,
                dept: dept
            },

            success: function (res) {
                if (!res || !res.success) {
                    notifyError(res && res.message ? res.message : "Không xóa được quyền");
                    return;
                }

                notifySuccess(res.message || "Đã xóa quyền thành công");

                loadRoles(employeeCd);
                loadAccounts();
            },

            error: function () {
                notifyError("Có lỗi xảy ra khi xóa quyền");
            }
        });
    }

    /* =========================================================
       RESET PASSWORD
    ========================================================= */

    function openResetPasswordModal(employeeCd, employeeName) {
        $resetEmployeeCd.val(employeeCd);
        $resetPasswordEmployeeText.text(`${employeeCd} - ${employeeName}`);
        $resetNewPassword.val("");

        if (resetPasswordModal) {
            resetPasswordModal.show();
        }
    }

    function resetPassword() {
        const employeeCd = $resetEmployeeCd.val();
        const newPassword = $resetNewPassword.val();

        if (!employeeCd) {
            notifyWarning("Thiếu mã nhân viên");
            return;
        }

        if (!newPassword) {
            notifyWarning("Vui lòng nhập mật khẩu mới");
            return;
        }

        const $button = $("#btnResetPassword");

        setButtonLoading($button, "Đang reset...");

        $.ajax({
            url: window.accountUrls.resetPassword,
            type: "POST",
            data: {
                EmployeeCD: employeeCd,
                NewPassword: newPassword
            },

            success: function (res) {
                if (!res || !res.success) {
                    notifyError(res && res.message ? res.message : "Không reset được mật khẩu");
                    return;
                }

                notifySuccess(res.message || "Đã reset mật khẩu thành công");

                if (resetPasswordModal) {
                    resetPasswordModal.hide();
                }

                loadAccounts();

                if (typeof window.refreshNotifications === "function") {
                    window.refreshNotifications();
                }
            },

            error: function () {
                notifyError("Có lỗi xảy ra khi reset mật khẩu");
            },

            complete: function () {
                resetButton($button, `<i class="fa-solid fa-rotate-right me-1"></i> Reset mật khẩu`);
            }
        });
    }

    /* =========================================================
       CREATE ACCOUNT
    ========================================================= */

    function openCreateAccountModal(employeeCd, employeeName) {
        $createEmployeeCd.val(employeeCd);
        $createAccountEmployeeText.text(`${employeeCd} - ${employeeName}`);
        $createPassword.val("Tent@c");

        if (createAccountModal) {
            createAccountModal.show();
        }
    }

    function createAccount() {
        const employeeCd = $createEmployeeCd.val();
        const password = $createPassword.val();

        if (!employeeCd) {
            notifyWarning("Thiếu mã nhân viên");
            return;
        }

        if (!password) {
            notifyWarning("Vui lòng nhập mật khẩu ban đầu");
            return;
        }

        const $button = $("#btnCreateAccount");

        setButtonLoading($button, "Đang tạo...");

        $.ajax({
            url: window.accountUrls.createAccount,
            type: "POST",
            data: {
                employeeCd: employeeCd,
                password: password
            },

            success: function (res) {
                if (!res || !res.success) {
                    notifyError(res && res.message ? res.message : "Không tạo được tài khoản");
                    return;
                }

                notifySuccess(res.message || "Đã tạo tài khoản thành công");

                if (createAccountModal) {
                    createAccountModal.hide();
                }

                loadAccounts();
            },

            error: function () {
                notifyError("Có lỗi xảy ra khi tạo tài khoản");
            },

            complete: function () {
                resetButton($button, `<i class="fa-solid fa-user-plus me-1"></i> Tạo tài khoản`);
            }
        });
    }

    /* =========================================================
       HELPERS
    ========================================================= */

    function togglePassword($input, $button) {
        const isPassword = $input.attr("type") === "password";

        $input.attr("type", isPassword ? "text" : "password");

        $button.html(
            isPassword
                ? `<i class="fa-solid fa-eye-slash"></i>`
                : `<i class="fa-solid fa-eye"></i>`
        );
    }

    function setButtonLoading($button, text) {
        $button
            .prop("disabled", true)
            .data("original-html", $button.html())
            .html(`
                <span class="spinner-border spinner-border-sm me-1"></span>
                ${text}
            `);
    }

    function resetButton($button, fallbackHtml) {
        const original = $button.data("original-html");

        $button
            .prop("disabled", false)
            .html(original || fallbackHtml);
    }

    function notifySuccess(message) {
        if (typeof toastr !== "undefined") {
            toastr.success(message);
            return;
        }

        alert(message);
    }

    function notifyError(message) {
        if (typeof toastr !== "undefined") {
            toastr.error(message);
            return;
        }

        alert(message);
    }

    function notifyWarning(message) {
        if (typeof toastr !== "undefined") {
            toastr.warning(message);
            return;
        }

        alert(message);
    }

    function escapeHtml(text) {
        return $("<div>")
            .text(text === null || text === undefined ? "" : text)
            .html();
    }

    function escapeAttr(text) {
        return escapeHtml(text)
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
});