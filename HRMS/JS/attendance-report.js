$(function () {

    const $form = $("#attendanceReportForm");

    if ($form.length === 0) {
        return;
    }

    if (!window.attendanceReportUrls) {
        console.error("Thiếu window.attendanceReportUrls.");
        return;
    }

    const $month = $("#attendanceMonth");
    const $dept = $("#attendanceDept");
    const $employee = $("#attendanceEmployee");
    const $status = $("#attendanceStatus");

    const $tbody = $("#attendanceTableBody");

    const $kpiTotalEmployees = $("#kpiTotalEmployees");
    const $kpiLateIn = $("#kpiLateIn");
    const $kpiMissing = $("#kpiMissing");
    const $kpiWorkOff = $("#kpiWorkOff");

    let loadTimer = null;

    // =========================
    // EVENTS
    // =========================

    $form.on("submit", function (e) {
        e.preventDefault();
        loadAttendanceReport();
    });

    $month.on("change", function () {
        scheduleLoad();
    });

    $dept.on("change", function () {
        scheduleLoad();
    });

    $status.on("change", function () {
        scheduleLoad();
    });

    $employee.on("keypress", function (e) {
        if (e.which === 13) {
            e.preventDefault();
            loadAttendanceReport();
        }
    });

    // =========================
    // LOAD
    // =========================

    function scheduleLoad() {
        clearTimeout(loadTimer);

        loadTimer = setTimeout(function () {
            loadAttendanceReport();
        }, 200);
    }

    function loadAttendanceReport() {
        const params = getParams();

        setLoading(true);

        $.ajax({
            url: window.attendanceReportUrls.data,
            type: "GET",
            data: params,

            success: function (res) {
                if (!res || !res.success) {
                    renderNoData();

                    toastr.error(
                        res && res.message
                            ? res.message
                            : "Không tải được dữ liệu chấm công"
                    );

                    return;
                }

                updateSummary(res.summary);

                renderTable(res.rows || []);
            },

            error: function () {
                renderNoData();
                toastr.error("Có lỗi xảy ra khi tải dữ liệu chấm công");
            },

            complete: function () {
                setLoading(false);
            }
        });
    }

    function getParams() {
        return {
            month: $month.val(),
            dept: $dept.val(),
            employee: $employee.val(),
            status: $status.val()
        };
    }

    // =========================
    // SUMMARY
    // =========================

    function updateSummary(summary) {
        summary = summary || {
            totalEmployees: 0,
            totalLateIn: 0,
            totalMissing: 0,
            totalWorkOnOffDay: 0
        };

        $kpiTotalEmployees.text(summary.totalEmployees || 0);
        $kpiLateIn.text(summary.totalLateIn || 0);
        $kpiMissing.text(summary.totalMissing || 0);
        $kpiWorkOff.text(summary.totalWorkOnOffDay || 0);
    }

    // =========================
    // TABLE
    // =========================

    function renderTable(rows) {
        if (!rows || rows.length === 0) {
            renderNoData();
            return;
        }

        let html = "";
        let currentDept = "";

        rows.forEach(function (row) {
            if (currentDept !== row.TenPhongBan) {
                currentDept = row.TenPhongBan;

                html += `
                <tr class="dept-row">
                    <td class="dept-name-cell">
                        <i class="fa-solid fa-folder-open me-2"></i>
                        ${escapeHtml(currentDept || "")}
                    </td>
                    <td class="dept-fill-cell" colspan="34"></td>
                </tr>
                `;
            }

            html += `
                <tr>
                    <td class="employee-cell">
                        <strong>${escapeHtml(row.EmployeeCD || "")}</strong>
                        <span>${escapeHtml(row.TenNhanVien || "")}</span>
                    </td>
            `;

            for (let i = 0; i < 31; i++) {
                const cell =
                    row.Days && row.Days[i]
                        ? row.Days[i]
                        : null;

                html += renderDayCell(cell);
            }

            html += `
                    <td class="fw-bold">${row.WorkingDays || 0}</td>
                    <td class="fw-bold">${formatNumber(row.TotalHours || 0)}</td>
                    <td class="fw-bold">${row.IssueCount || 0}</td>
                </tr>
            `;
        });

        $tbody.html(html);
    }

    function renderDayCell(cell) {
        if (!cell || cell.StatusCode === "EMPTY") {
            return `<td></td>`;
        }

        const statusClass = "attendance-cell status-" + cell.StatusCode;

        const title = buildTooltip(cell);

        const symbol = cell.Symbol || "";

        return `
            <td>
                <span class="${statusClass}"
                      title="${escapeAttr(title)}">
                    ${escapeHtml(symbol)}
                </span>
            </td>
        `;
    }

    function renderNoData() {
        $tbody.html(`
            <tr>
                <td colspan="35"
                    class="text-center py-4 text-muted">
                    Không có dữ liệu
                </td>
            </tr>
        `);

        updateSummary(null);
    }

    // =========================
    // TOOLTIP
    // =========================

    function buildTooltip(cell) {
        const parts = [];

        if (cell.Date) {
            parts.push("Ngày: " + cell.Date);
        }

        if (cell.ShiftName) {
            let shiftText = "Ca: " + cell.ShiftName;

            if (cell.ShiftTimeText) {
                shiftText += " (" + cell.ShiftTimeText + ")";
            }

            parts.push(shiftText);
        }

        if (cell.ShiftSource) {
            let sourceText = cell.ShiftSource;

            if (cell.ShiftSource === "REGISTERED") {
                sourceText = "Đăng ký ca";
            }
            else if (cell.ShiftSource === "DEFAULT_DEPARTMENT") {
                sourceText = "Ca mặc định phòng ban";
            }
            else if (cell.ShiftSource === "FALLBACK") {
                sourceText = "Ca mặc định hệ thống";
            }

            parts.push("Nguồn ca: " + sourceText);
        }

        if (cell.FirstCheckIn) {
            parts.push("Vào: " + cell.FirstCheckIn);
        }
        else {
            parts.push("Vào: --");
        }

        if (cell.LastCheckOut) {
            parts.push("Ra: " + cell.LastCheckOut);
        }
        else {
            parts.push("Ra: --");
        }

        if (cell.WorkingHours && cell.WorkingHours > 0) {
            parts.push("Giờ: " + formatNumber(cell.WorkingHours));
        }

        if (cell.StatusText) {
            parts.push("Trạng thái: " + cell.StatusText);
        }

        if (cell.Note) {
            parts.push("Ghi chú: " + cell.Note);
        }

        return parts.join("\n");
    }

    // =========================
    // LOADING
    // =========================

    function setLoading(isLoading) {
        const $card = $(".attendance-table-card");
        const $button = $(".btn-attendance-search");

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
                Xem báo cáo
            `);
    }

    // =========================
    // HELPERS
    // =========================

    function formatNumber(value) {
        const number = parseFloat(value || 0);

        if (Number.isInteger(number)) {
            return number.toString();
        }

        return number.toFixed(2);
    }

    function escapeHtml(text) {
        return $("<div>")
            .text(text || "")
            .html();
    }

    function escapeAttr(text) {
        return String(text || "")
            .replace(/&/g, "&amp;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;");
    }

});