const days = 31;

function createHeader() {
    let html = "<tr><th style='min-width:230px'>Phòng ban / Nhân viên</th>";
    for (let i = 1; i <= days; i++) html += `<th>${i}</th>`;
    html += "<th>Tổng</th></tr>";
    $("#tableHead").html(html);
}

function loadData(month, year) {
    $.getJSON("/Reports/GetOvertimePivot?month=" + month + "&year=" + year, function (data) {
        renderTable(data);
        updateSummary(data);
    }).fail(function () {
        alert("Không lấy được dữ liệu từ server");
    });
}

function renderTable(data) {
    let body = "";
    let currentDept = "";

    data.forEach(row => {
        if (!row.TenNhanVien) {
            body += `<tr class="totalRow"><td>Tổng phòng</td>`;
            for (let d = 1; d <= days; d++) body += `<td>${row["N" + d] || ""}</td>`;
            body += `<td>${row.TongPB || ""}</td></tr>`;
            return;
        }

        if (currentDept !== row.TenPhongBan) {
            currentDept = row.TenPhongBan;
            body += `<tr class="department"><td colspan="${days + 2}">📁 ${currentDept}</td></tr>`;
        }

        body += "<tr class='employee'>";
        body += `<td style="text-align:left;padding-left:25px;">👤 ${row.TenNhanVien}</td>`;
        for (let d = 1; d <= days; d++) {
            const value = row["N" + d];
            body += `<td>${value === 0 || value == null ? "" : value}</td>`;
        }
        body += `<td><b>${row.TongSoGioTangCa}</b></td></tr>`;
    });

    $("#tableBody").html(body);
}

function updateSummary(data) {
    const employees = data.filter(r => r.TenNhanVien);
    const totalEmployees = new Set(employees.map(r => r.EmployeeCD)).size;
    const totalSessions = employees.length;
    const totalHours = employees.reduce((sum, r) => sum + (r.TongSoGioTangCa || 0), 0);

    let dayTotals = Array(days).fill(0);
    employees.forEach(r => {
        for (let d = 1; d <= days; d++) dayTotals[d - 1] += (r["N" + d] || 0);
    });
    const maxDay = dayTotals.indexOf(Math.max(...dayTotals)) + 1;

    $("#totalEmployees").text(totalEmployees);
    $("#totalSessions").text(totalSessions);
    $("#totalHours").text(totalHours);
    $("#maxDay").text(maxDay);
}

$(document).ready(function () {
    createHeader();

    // Auto load dữ liệu tháng hiện tại khi vào trang
    const today = new Date();
    const month = today.getMonth() + 1;
    const year = today.getFullYear();
    loadData(month, year);

    // Nếu người dùng bấm nút filter thì load lại
    $("#btnLoad").click(function () {
        const val = $("#monthInput").val();
        if (!val) return;
        const [year, month] = val.split("-");
        loadData(month, year);
    });
});
