$(function () {

    const $form = $("#reportFilterForm");
    if ($form.length === 0)
    {
        return;
    }
    const $month = $("#reportMonth");
    const $dept = $("#reportDept");
    const $employee = $("#reportEmployee");

    const $tbody = $("#reportTableBody");
    const $export = $("#btnExportOvertime");

    const $kpiTotalEmployees = $("#kpiTotalEmployees");
    const $kpiTotalSessions = $("#kpiTotalSessions");
    const $kpiTotalHours = $("#kpiTotalHours");
    const $kpiMaxDay = $("#kpiMaxDay");

    // =========================
    // EVENTS
    // =========================

    $form.on("submit", function (e) {
        e.preventDefault();
        loadReport();
    });

    $month.on("change", function () {
        loadReport();
    });

    $dept.on("change", function () {
        loadReport();
    });

    $employee.on("keypress", function (e) {
        if (e.which === 13) {
            e.preventDefault();
            loadReport();
        }
    });

    // =========================
    // LOAD REPORT
    // =========================

    function loadReport() {
        const params = getFilterParams();
        setLoading(true);
        $.ajax({
            url: window.reportOvertimeUrls.search,
            type: "GET",
            data: params,
            success: function (res) {
                if (!res || !res.success) {
                    renderNoData();
                    toastr.error(
                        res && res.message
                            ? res.message
                            : "Không tải được dữ liệu báo cáo"
                    );
                    return;
                }

                updateSummary(res.summary);
                renderTable(
                    res.rows || [],
                    res.totalRow
                );
                updateExportLink(params);
            },

            error: function () {
                renderNoData();
                toastr.error("Có lỗi xảy ra khi tải báo cáo");
            },

            complete: function () {
                setLoading(false);
            }
        });
    }

    // =========================
    // FILTER
    // =========================

    function getFilterParams() {
        return {
            month: $month.val(),
            dept: $dept.val(),
            employee: $employee.val()
        };
    }

    function updateExportLink(params) {
        const query =
            "?month=" + encodeURIComponent(params.month || "") +
            "&dept=" + encodeURIComponent(params.dept || "") +
            "&employee=" + encodeURIComponent(params.employee || "");
        $export.attr(
            "href",
            window.reportOvertimeUrls.export + query
        );
    }

    // =========================
    // SUMMARY
    // =========================

    function updateSummary(summary) {
        if (!summary) {
            summary = {
                totalEmployees: 0,
                totalSessions: 0,
                totalHours: 0,
                maxDay: 0
            };
        }
        $kpiTotalEmployees.text(summary.totalEmployees || 0);
        $kpiTotalSessions.text(summary.totalSessions || 0);
        $kpiTotalHours.text(formatNumber(summary.totalHours || 0));
        $kpiMaxDay.text(summary.maxDay || 0);
    }

    // =========================
    // TABLE
    // =========================

    function renderTable(rows, totalRow) {
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
                        <td colspan="33">
                            <i class="fas fa-folder-open me-2"></i>
                            ${escapeHtml(currentDept || "")}
                        </td>
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

                const value = row.Ngay && row.Ngay[i] ? row.Ngay[i] : 0;
                html += `
                    <td>
                        ${value > 0 ? formatNumber(value) : ""}
                    </td>
                `;
            }

            html += `
                    <td class="fw-bold">
                        ${formatNumber(row.TongSoGioTangCa || 0)}
                    </td>
                </tr>
            `;
        });

        if (totalRow) {
            html += `
                <tr class="total-row">
                    <td>
                        ${escapeHtml(totalRow.TenPhongBan || "Tổng cộng")}
                    </td>
            `;

            for (let i = 0; i < 31; i++)
            {
                const value =
                    totalRow.Ngay && totalRow.Ngay[i] ? totalRow.Ngay[i] : 0;
                html += `
                    <td>
                        ${value > 0 ? formatNumber(value) : ""}
                    </td>
                `;
            }

            html += `
                    <td>
                        ${formatNumber(totalRow.TongSoGioTangCa || 0)}
                    </td>
                </tr>
            `;
        }
        $tbody.html(html);
    }

    function renderNoData() {
        $tbody.html(`
            <tr>
                <td colspan="33"
                    class="text-center py-4 text-muted">
                    Không có dữ liệu
                </td>
            </tr>
        `);
        updateSummary(null);
    }

    // =========================
    // LOADING
    // =========================

    function setLoading(isLoading) {
        const $tableCard = $(".report-table-card");
        if (isLoading)
        {
            $tableCard.addClass("is-loading");
            $(".btn-report-search")
                .prop("disabled", true)
                .html(`
                    <span class="spinner-border spinner-border-sm me-1"></span>
                    Đang tải
                `);
        }
        else
        {
            $tableCard.removeClass("is-loading");
            $(".btn-report-search")
                .prop("disabled", false)
                .html(`
                    <i class="fas fa-search me-1"></i>
                    Xem báo cáo
                `);
        }
    }

    // =========================
    // HELPERS
    // =========================

    function formatNumber(value)
    {
        const number = parseFloat(value || 0);
        if (Number.isInteger(number))
        {
            return number.toString();
        }
        return number.toFixed(2);
    }

    function escapeHtml(text)
    {
        return $("<div>")
            .text(text || "")
            .html();
    }

});