$(function () {
    const $form = $("#employeeFilterForm");

    if ($form.length === 0) {
        return;
    }

    const $keyword = $("#employeeKeyword");
    const $dept = $("#employeeDept");
    const $status = $("#employeeStatus");

    const $tbody = $("#employeeTableBody");

    const $kpiTotalEmployees = $("#kpiTotalEmployees");
    const $kpiTotalDepartments = $("#kpiTotalDepartments");
    const $kpiNewEmployees = $("#kpiNewEmployees");
    const $kpiInsurance = $("#kpiInsurance");

    let searchTimer = null;
    let currentRequest = null;

    $form.on("submit", function (e) {
        e.preventDefault();
        loadEmployees();
    });

    $dept.on("change", function () {
        loadEmployees();
    });

    $status.on("change", function () {
        loadEmployees();
    });

    $keyword.on("input", function () {
        clearTimeout(searchTimer);

        searchTimer = setTimeout(function () {
            loadEmployees();
        }, 350);
    });

    function loadEmployees() {
        const params = {
            keyword: $.trim($keyword.val()),
            dept: $dept.val(),
            status: $status.val()
        };

        if (currentRequest) {
            currentRequest.abort();
        }

        setLoading(true);

        currentRequest = $.ajax({
            url: window.employeeUrls.data,
            type: "GET",
            data: params,
            cache: false,

            success: function (res) {
                if (!res || !res.success) {
                    if (typeof toastr !== "undefined") {
                        toastr.error(res && res.message ? res.message : "Không tải được danh sách nhân viên");
                    }

                    renderNoData();
                    return;
                }

                updateSummary(res.summary);
                renderTable(res.rows || []);
            },

            error: function (xhr, status) {
                if (status === "abort") {
                    return;
                }

                if (typeof toastr !== "undefined") {
                    toastr.error("Có lỗi xảy ra khi tải danh sách nhân viên");
                }

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
            totalEmployees: 0,
            totalDepartments: 0,
            totalNewEmployees: 0,
            totalInsurance: 0
        };

        $kpiTotalEmployees.text(summary.totalEmployees || 0);
        $kpiTotalDepartments.text(summary.totalDepartments || 0);
        $kpiNewEmployees.text(summary.totalNewEmployees || 0);
        $kpiInsurance.text(summary.totalInsurance || 0);
    }

    function renderTable(rows) {
        if (!rows || rows.length === 0) {
            renderNoData();
            return;
        }

        let html = "";

        rows.forEach(function (emp) {
            const employeeCd = emp.EmployeeCD || "";

            const detailUrl =
                window.employeeUrls.detail +
                "?employeeCd=" +
                encodeURIComponent(employeeCd);

            const statusText = emp.TrangThai || "Đang làm việc";

            let statusClass = "";

            if (statusText === "Tạm nghỉ") {
                statusClass = "status-paused";
            }
            else if (statusText === "Ngưng kích hoạt") {
                statusClass = "status-disabled";
            }

            html += `
                <tr>
                    <td class="fw-bold">${escapeHtml(employeeCd)}</td>

                    <td>
                        <div class="employee-name">
                            <strong>${escapeHtml(emp.TenNhanVien || "")}</strong>
                            <span>${escapeHtml(employeeCd)}</span>
                        </div>
                    </td>

                    <td>${escapeHtml(emp.MaChamCong || "")}</td>

                    <td>${escapeHtml(emp.MaThe || "")}</td>

                    <td>${escapeHtml(emp.TenPhongBan || "")}</td>

                    <td>${escapeHtml(emp.NgayVaoLamViec || "")}</td>

                    <td>
                        ${emp.DangThamGiaBaoHiem
                    ? '<span class="employee-badge badge-success">Có</span>'
                    : '<span class="employee-badge badge-muted">Không</span>'
                }
                    </td>

                    <td>
                        <span class="employee-status ${statusClass}">
                            ${escapeHtml(statusText)}
                        </span>
                    </td>
                    <td>
                        <a href="${detailUrl}" class="btn btn-sm btn-outline-primary"> 
                            <i class="fa-solid fa-eye me-1">
                             </i> Xem 
                        </a> 
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
                    Không có dữ liệu nhân viên
                </td>
            </tr>
        `);

        updateSummary(null);
    }

    function setLoading(isLoading) {
        const $card = $(".employees-table-card");
        const $button = $(".btn-employee-search");

        if (isLoading) {
            $card.addClass("is-loading");

            $button
                .prop("disabled", true)
                .html(`
                    <span class="spinner-border spinner-border-sm me-1"></span>
                    Đang tải
                `);

            return;
        }

        $card.removeClass("is-loading");

        $button
            .prop("disabled", false)
            .html(`
                <i class="fa-solid fa-search me-1"></i>
                Tìm kiếm
            `);
    }

    function escapeHtml(text) {
        return $("<div>")
            .text(text || "")
            .html();
    }
});