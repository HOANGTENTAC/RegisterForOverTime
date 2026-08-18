// pagination.js
function paginateTable(tableId, rowsPerPage, paginationContainerId) {
    const table = document.querySelector(`#${tableId} tbody`);
    if (!table) return;

    const rows = table.querySelectorAll("tr");
    const totalPages = Math.ceil(rows.length / rowsPerPage);
    const pagination = document.getElementById(paginationContainerId);
    let currentPage = 1;

    function showPage(page) {
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;
        currentPage = page;

        rows.forEach((row, index) => {
            row.style.display =
                index >= (page - 1) * rowsPerPage && index < page * rowsPerPage
                    ? ""
                    : "none";
        });

        renderPagination();
    }

    function renderPagination() {
        pagination.innerHTML = "";

        // Prev
        const prevBtn = document.createElement("button");
        prevBtn.textContent = "Prev";
        prevBtn.disabled = currentPage === 1;
        prevBtn.addEventListener("click", () => showPage(currentPage - 1));
        pagination.appendChild(prevBtn);

        // Page numbers
        for (let i = 1; i <= totalPages; i++) {
            const btn = document.createElement("button");
            btn.textContent = i;
            btn.classList.toggle("active", i === currentPage);
            btn.addEventListener("click", () => showPage(i));
            pagination.appendChild(btn);
        }

        // Next
        const nextBtn = document.createElement("button");
        nextBtn.textContent = "Next";
        nextBtn.disabled = currentPage === totalPages;
        nextBtn.addEventListener("click", () => showPage(currentPage + 1));
        pagination.appendChild(nextBtn);
    }

    showPage(1);
}
