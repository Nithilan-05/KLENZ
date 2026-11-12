function exportToPDF(headers, data, columnWidths, fileName) {
    //alert(data);
    const doc = new window.jspdf.jsPDF({
        orientation: "landscape",
        unit: "mm",
        format: "a4"
    });

    // Title
    doc.setFont("helvetica", "bold");
    doc.setFontSize(16);
    doc.text(fileName.replace(".pdf", ""), 120, 10);

    // Column styles based on dynamic widths
    let columnStyles = {};
    columnWidths.forEach((width, index) => {
        columnStyles[index] = { cellWidth: width };
    });

    // Generate table in PDF
    doc.autoTable({
        head: [headers],
        body: data,
        startY: 15,
        theme: 'grid',
        margin: { left: 10, right: 10 },
        styles: { fontSize: 9, cellPadding: 2 },
        headStyles: { fillColor: [40, 40, 40], textColor: [255, 255, 255] },
        columnStyles: columnStyles // Apply dynamic column widths
    });

    doc.save(fileName);
}
function extractTableData(excludeFirstLast = false) {
    let table = $('.datatable').DataTable(); // Get DataTable instance

    // Temporarily disable pagination to fetch all rows
    var klenzPrevLen = table.page.len();
    table.page.len(-1).draw();

    let tableData = [];
    document.querySelectorAll(".datatable tbody tr").forEach((row, index) => {
        let rowData = [index + 1]; // Serial Number
        row.querySelectorAll("td").forEach((cell, cellIndex) => {
            if (!excludeFirstLast || cellIndex !== 0) {
                rowData.push(cell.textContent.trim() || "-");
            }

            //if (!excludeFirstLast || (cellIndex !== 0 && cellIndex !== cells.length - 1)) {
            //    rowData.push(cell.textContent.trim() || "-"); // Replace empty cells with "-"
            //}
        });
        tableData.push(rowData);
    });

    // Restore pagination settings
    table.page.len(klenzPrevLen).draw(); // Set back to 5 or your preferred number

    return tableData;
}
function exportToExcel(headers, fileName) {
    let workbook = new ExcelJS.Workbook();
    let worksheet = workbook.addWorksheet(fileName.replace('.xlsx', ''));

    let headerRow = worksheet.addRow(headers);
    headerRow.font = { bold: true, size: 14 };
    headerRow.alignment = { horizontal: 'center' };

    headerRow.eachCell(cell => {
        cell.border = {
            top: { style: 'thin' },
            left: { style: 'thin' },
            bottom: { style: 'thin' },
            right: { style: 'thin' }
        };
        cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFFFCC00' } };
    });

    let table = $('.datatable').DataTable();
    var klenzPrevLen = table.page.len();
    table.page.len(-1).draw(); // Fetch all rows

    let rowIndex = 1;
    document.querySelectorAll(".datatable tbody tr").forEach(row => {
        let rowData = [rowIndex]; // Serial Number

        row.querySelectorAll("td:not(:first-child)").forEach(cell => {
            rowData.push(cell.textContent.trim() === "" ? "-" : cell.textContent.trim()); 
        }); 

        let dataRow = worksheet.addRow(rowData);

        // Apply border to all cells, including last column (Remarks)
        dataRow.eachCell((cell) => {
            cell.border = {
                top: { style: 'thin' },
                left: { style: 'thin' },
                bottom: { style: 'thin' },
                right: { style: 'thin' }
            };
        });

        rowIndex++;
    });

    table.page.len(klenzPrevLen).draw(); // Restore pagination

    worksheet.columns.forEach((column, index) => {
        column.width = index === 0 ? 8 : 20;
        if (index === 0) column.alignment = { horizontal: "center" };
    });

    workbook.xlsx.writeBuffer().then(buffer => {
        let blob = new Blob([buffer], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" });
        saveAs(blob, fileName);
    });
}




document.addEventListener("DOMContentLoaded", function () {
   
    const pdfButton_PositiveEnq = document.getElementById('exportPdf_PositiveEnquiry');
    if (pdfButton_PositiveEnq) {
        pdfButton_PositiveEnq.addEventListener('click', function () {
            const headers = ["S.No", "Quotation Date", "Company Name", "Product Details", "Customer Details", "Values", "Current Status"];
            const columnWidths = [12, 20, 50, 60, 45, 30, 30];
            const data = extractTableData(true);
            //alert(data);
            exportToPDF(headers, data, columnWidths, "Positive Enquiry Report.pdf");
        });
    }

    const excelButton_PositiveEnq = document.getElementById('exportExcel_PositiveEnquiry');
    if (excelButton_PositiveEnq) {
        excelButton_PositiveEnq.addEventListener('click', function () {
            const headers = ["S.No", "Quotation Date", "Company Name", "Product Details", "Customer Details", "Value", "Current Status"];
            exportToExcel(headers, "Positive Enquiry Report.xlsx");
        });
    }
    
    const pdfButton = document.getElementById('exportPdf_QuotationReport');
    if (pdfButton) {
        pdfButton.addEventListener('click', function () {
            const headers = ["S.No", "Quotation Date", "Company Name", "Product Details", "Customer Details", "Value", "Remarks"];
            const columnWidths = [12, 20, 50, 60, 45, 30, 30];
            const data = extractTableData(true);
            exportToPDF(headers, data, columnWidths, "Quotation Report.pdf");
        });
    }

    const excelButton = document.getElementById('exportExcel_QuotationReport');
    if (excelButton) {
        excelButton.addEventListener('click', function () {
            const headers = ["S.No", "Quotation Date", "Company Name", "Product Details", "Customer Details", "Value", "Remarks"];
            exportToExcel(headers, "Quotation Report.xlsx");
        });
    }

    const pdfButton_salesEnq = document.getElementById('exportPdf');
    if (pdfButton_salesEnq) {
        pdfButton_salesEnq.addEventListener('click', function () {
            const headers = ["S.No", "Enquiry Date", "Company Name", "Referred By", "Enquiry Details", "Customer Details", "Status", "Reminder Date", "Reminder Place"];
            const columnWidths = [12, 20, 40, 30, 50, 50, 25, 20, 30];
            const data = extractTableData(true);
            exportToPDF(headers, data, columnWidths, "Sales Enquiry.pdf");
        });
    }

    const excelButton_salesEnq = document.getElementById('exportExcel');
    if (excelButton_salesEnq) {
        excelButton_salesEnq.addEventListener('click', function () {
            const headers = ["S.No", "Enquiry Date", "Company Name", "Referred By", "Enquiry Details", "Customer Details", "Status", "Reminder Date", "Reminder Place"];
            exportToExcel(headers, "Sales Enquiry.xlsx");
        });
    }


    const pdfButton_projList = document.getElementById('exportPdf_ProjectList');
    if (pdfButton_projList) {
        pdfButton_projList.addEventListener('click', function () {
            const headers = ["S.No", "F.Y", "Work Order date", "Company Name", "Customer Details", "Work Details", "Work order value", "Remarks"];
            const columnWidths = [12, 30, 20, 50, 50, 50, 25, 30];
            const data = extractTableData(true);
            exportToPDF(headers, data, columnWidths, "Project List.pdf");
        });
    }

    const excelButton_projList = document.getElementById('exportExcel_ProjectList');
    if (excelButton_projList) {
        excelButton_projList.addEventListener('click', function () {
            const headers = ["S.No", "F.Y", "Work Order date", "Company Name", "Customer Details", "Work Details", "Work order value", "Remarks"];
            exportToExcel(headers, "Project List.xlsx");
        });
    }

    // Project Consultancy

    const pdfButton_projCons = document.getElementById('exportPdf_ProjectConsultancy');
    if (pdfButton_projCons) {
        pdfButton_projCons.addEventListener('click', function () {
            const headers = ["S.No", "F.Y", "Work Order date", "Company Details	", "Customer", "Work Details","Project Cost","GST","Total", "Work duration", "Remarks"];
            const columnWidths = [12, 23,       20                  , 50            , 50        , 20                , 25    , 12  ,  20    ,  18            ,    20];
            const data = extractTableData(true);
            exportToPDF(headers, data, columnWidths, "Project Consultancy.pdf");
        });
    }

    const excelButton_projCons = document.getElementById('exportExcel_ProjectConsultancy');
    if (excelButton_projCons) {
        excelButton_projCons.addEventListener('click', function () {
            const headers = ["S.No", "F.Y", "Work Order date", "Company Details	", "Customer", "Work Details", "Project Cost", "GST", "Total", "Work duration", "Remarks"];
            exportToExcel(headers, "Project Consultancy.xlsx");
        });
    }

    // Project Tender List 

    const pdfButton_projTender = document.getElementById('exportPdf_ProjectTenderList');
    if (pdfButton_projTender) {
        pdfButton_projTender.addEventListener('click', function () {
            const headers = ["S.No", "F.Y", "Work Order date", "Company Details	", "Customer", "Work Details", "Project Cost", "GST", "Total", "Work duration", "Remarks"];
            const columnWidths = [12, 23, 20, 50, 50, 20, 25, 12, 20, 18, 20];
            const data = extractTableData(true);
            exportToPDF(headers, data, columnWidths, "Project Tender List.pdf");
        });
    }

    const excelButton_projTender = document.getElementById('exportExcel_ProjectTenderList');
    if (excelButton_projTender) {
        excelButton_projTender.addEventListener('click', function () {
            const headers = ["S.No", "F.Y", "Work Order date", "Company Details	", "Customer", "Work Details", "Project Cost", "GST", "Total", "Work duration", "Remarks"];
            exportToExcel(headers, "Project Tender List.xlsx");
        });
    }

    // Project To Be Tender List 

    const pdfButton_projToBeTender = document.getElementById('exportPdf_ProjectToBeTenderList');
    if (pdfButton_projToBeTender) {
        pdfButton_projToBeTender.addEventListener('click', function () {
            const headers = ["S.No", "F.Y", "Work Order date", "Company Details	", "Customer", "Work Details", "Project Cost", "GST", "Total", "Work duration", "Remarks"];
            const columnWidths = [12, 23, 20, 50, 50, 20, 25, 12, 20, 18, 20];
            const data = extractTableData(true);
            exportToPDF(headers, data, columnWidths, "Project To Be Tender List.pdf");
        });
    }

    const excelButton_projToBeTender = document.getElementById('exportExcel_ProjectToBeTenderList');
    if (excelButton_projToBeTender) {
        excelButton_projToBeTender.addEventListener('click', function () {
            const headers = ["S.No", "F.Y", "Work Order date", "Company Details	", "Customer", "Work Details", "Project Cost", "GST", "Total", "Work duration", "Remarks"];
            exportToExcel(headers, "Project To Be Tender List.xlsx");
        });
    }

    // Klenz Chemicals

    const pdfButton_klenzChem = document.getElementById('exportPdf_ProjectKlenzChemicals');
    if (pdfButton_klenzChem) {
        pdfButton_klenzChem.addEventListener('click', function () {
            const headers = ["S.No", "F.Y", "Work Order date", "Company Details", "Customer", "Work Details", "Project Cost", "GST", "Total", "Work duration", "Remarks"];
            const columnWidths = [12, 23, 20, 50, 50, 20, 25, 12, 20, 18, 20];
            const data = extractTableData(true);
            exportToPDF(headers, data, columnWidths, "Klenz Chemicals Projects.pdf");
        });
    }

    const excelButton_klenzChem = document.getElementById('exportExcel_ProjectKlenzChemicals');
    if (excelButton_klenzChem) {
        excelButton_klenzChem.addEventListener('click', function () {
            const headers = ["S.No", "F.Y", "Work Order date", "Company Details", "Customer", "Work Details", "Project Cost", "GST", "Total", "Work duration", "Remarks"];
            exportToExcel(headers, "Klenz Chemicals Projects.xlsx");
        });
    }
});
