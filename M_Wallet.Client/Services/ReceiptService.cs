using System.Text;
using M_Wallet.Shared;

namespace M_Wallet.Client.Services
{
    /// <summary>
    /// Generates HTML for printing receipts and invoices.
    /// Supports thermal printer format (80mm) and A4 invoice format.
    /// </summary>
    public class ReceiptService
    {
        /// <summary>
        /// Generates thermal printer receipt HTML (80mm width).
        /// Used for point-of-sale receipt printing.
        /// </summary>
        /// <param name="transaction">The transaction to generate a receipt for.</param>
        /// <returns>HTML string formatted for thermal printers.</returns>
        public string GenerateReceiptHtml(Transaction transaction)
        {
            var sb = new StringBuilder();

            // Thermal Styles
            sb.Append(@"
<style>
    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; direction: rtl; text-align: right; padding: 0; margin: 0; font-size: 12px; color: #000; }
    .receipt-container { width: 100%; max-width: 80mm; margin: 0 auto; padding: 5px; box-sizing: border-box; }
    .header { text-align: center; margin-bottom: 10px; }
    .store-name { font-size: 20px; font-weight: bold; margin-bottom: 5px; }
    .meta { font-size: 11px; margin-bottom: 2px; }
    .divider { border-top: 1px dashed #000; margin: 8px 0; }
    table { width: 100%; border-collapse: collapse; margin-bottom: 5px; }
    th { text-align: right; font-size: 11px; border-bottom: 1px solid #000; padding-bottom: 2px; }
    td { text-align: right; font-size: 12px; padding: 4px 0; vertical-align: top; }
    .qty { width: 30px; text-align: center; }
    .price { width: 50px; text-align: left; }
    .total-section { margin-top: 10px; border-top: 1px solid #000; padding-top: 5px; }
    .total-row { display: flex; justify-content: space-between; font-weight: bold; font-size: 16px; margin-bottom: 5px; }
    .sub-row { display: flex; justify-content: space-between; font-size: 12px; margin-bottom: 2px; }
    .footer { margin-top: 15px; font-size: 12px; text-align: center; font-weight: bold; }
    @media print { @page { margin: 0; } body { margin: 0; } }
</style>");

            sb.Append("<div class='receipt-container'>");

            // Header
            sb.Append("<div class='header'>");
            sb.Append("<div class='store-name'>Draidy - دريدي</div>");
            sb.Append($"<div class='meta'>{transaction.TransactionDate.ToLocalTime():yyyy/MM/dd hh:mm tt}</div>");
            sb.Append($"<div class='meta'>#{transaction.Id}</div>");
            if (!string.IsNullOrEmpty(transaction.CustomerName))
            {
                sb.Append($"<div class='meta'>العميل: {transaction.CustomerName}</div>");
            }
            sb.Append($"<div class='meta'>الموظف: {transaction.EmployeeName}</div>");
            sb.Append("</div>");

            sb.Append("<div class='divider'></div>");

            // Items Table
            sb.Append("<table>");
            sb.Append("<thead><tr><th style='text-align:right'>الصنف</th><th class='qty'>العدد</th><th class='price'>السعر</th></tr></thead>");
            sb.Append("<tbody>");

            foreach (var item in transaction.Items)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{item.Product?.Name ?? "Unknown"}</td>");
                sb.Append($"<td class='qty'>{item.Quantity}</td>");
                sb.Append($"<td class='price'>{item.Subtotal:F2}</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
            sb.Append("<div class='divider'></div>");

            // Totals
            sb.Append("<div class='total-section'>");
            
            if (transaction.Discount > 0)
            {
                 var subtotal = transaction.TotalAmount + transaction.Discount;
                 sb.Append($"<div class='sub-row'><span>{subtotal:F2}</span><span>المجموع</span></div>");
                 sb.Append($"<div class='sub-row'><span>{transaction.Discount:F2}</span><span>الخصم</span></div>");
            }

            sb.Append($"<div class='total-row'><span>{transaction.TotalAmount:F2}</span><span>الإجمالي</span></div>");
            
            // Calculate Paid Amount
            if (transaction.TotalPaid > 0)
            {
                sb.Append($"<div class='sub-row'><span>{transaction.TotalPaid:F2}</span><span>المدفوع</span></div>");
                if (transaction.BalanceDue > 0.01m)
                {
                    sb.Append($"<div class='sub-row'><span>{transaction.BalanceDue:F2}</span><span>المتبقي</span></div>");
                }
            }
            
            sb.Append("</div>");

            // Footer
            sb.Append("<div class='footer'>");
            sb.Append("شكراً لزيارتكم");
            sb.Append("</div>");

            sb.Append("</div>"); // End receipt-container

            return sb.ToString();
        }

        /// <summary>
        /// Generates A4 invoice HTML with full company branding.
        /// Used for formal invoice printing and PDF generation.
        /// </summary>
        /// <param name="transaction">The transaction to generate an invoice for.</param>
        /// <returns>HTML string formatted for A4 paper.</returns>
        public string GenerateInvoiceHtml(Transaction transaction)
        {
            var sb = new StringBuilder();
            // A4 Styles
            sb.Append(@"
<style>
    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; direction: rtl; text-align: right; color: #333; background: #f5f5f5; margin: 0; }
    .a4-container { width: 210mm; min-height: 280mm; padding: 10mm; margin: 20px auto; background: white; box-shadow: 0 0 20px rgba(0,0,0,0.1); box-sizing: border-box; position: relative; }
    
    .header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 30px; border-bottom: 3px solid #34495e; padding-bottom: 15px; }
    .company-info h1 { margin: 0 0 5px 0; color: #2c3e50; font-size: 32px; font-weight: 800; letter-spacing: -1px; }
    .company-info p { margin: 2px 0; color: #666; font-size: 14px; }
    
    .invoice-details { text-align: right; }
    .invoice-details h2 { margin: 0 0 10px 0; color: #34495e; font-size: 24px; font-weight: 700; }
    .invoice-meta { font-size: 14px; color: #555; line-height: 1.5; }
    
    .bill-to { margin-bottom: 30px; background: #f8f9fa; padding: 20px; border-radius: 8px; border-right: 5px solid #34495e; }
    .bill-to h3 { margin-top: 0; margin-bottom: 10px; font-size: 18px; color: #2c3e50; }
    .bill-to div { margin-bottom: 5px; font-size: 15px; }
    
    table { width: 100%; border-collapse: collapse; margin-bottom: 30px; table-layout: fixed; }
    th { background: #34495e; color: white; padding: 12px; font-size: 15px; text-align: center; font-weight: 600; border: 1px solid #34495e; }
    td { padding: 10px; border: 1px solid #ddd; font-size: 15px; text-align: center; color: #444; }
    tr:nth-child(even) { background-color: #fcfcfc; }
    
    /* Column Widths */
    th:nth-child(1) { width: 40%; text-align: right; padding-right: 15px; } /* Product */
    th:nth-child(2) { width: 15%; } /* Qty */
    th:nth-child(3) { width: 20%; } /* Price */
    th:nth-child(4) { width: 25%; } /* Total */
    
    td:nth-child(1) { text-align: right; padding-right: 15px; }
    
    .totals { margin-right: auto; margin-left: 0; width: 40%; background: #fff; padding: 0; }
    .total-row { display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #eee; font-size: 15px; }
    .total-row span:first-child { color: #666; font-weight: 500; }
    .total-row span:last-child { color: #333; font-weight: 700; }
    .total-row.final { border-bottom: none; border-top: 3px solid #34495e; margin-top: 10px; padding-top: 10px; font-size: 20px; color: #2c3e50; }
    
    .footer { position: absolute; bottom: 0; left: 10mm; right: 10mm; text-align: center; color: #95a5a6; font-size: 13px; border-top: 1px solid #eee; padding-top: 15px; padding-bottom: 10mm; }
    
    @media print { 
        @page { size: A4; margin: 0; }
        body { margin: 0; padding: 0; background: #fff; } 
        .a4-container { width: auto; max-width: 100%; border: none; margin: 0; padding: 10mm; box-shadow: none; min-height: 260mm; overflow: hidden; page-break-inside: avoid; } 
        .bill-to { background: none; border: 1px solid #eee; border-right: 5px solid #34495e; }
        th { background-color: #34495e !important; color: #fff !important; -webkit-print-color-adjust: exact; }
    }
</style>");

            sb.Append("<div class='a4-container'>");
            
            // Header
            sb.Append("<div class='header'>");
            
            // Invoice Details (Right)
            sb.Append("<div class='invoice-details'>");
            sb.Append("<h2>فاتورة مبيعات</h2>");
            sb.Append($"<div class='invoice-meta'><div><strong>رقم الفاتورة:</strong> #{transaction.Id}</div>");
            sb.Append($"<div><strong>التاريخ:</strong> {transaction.TransactionDate.ToLocalTime():yyyy/MM/dd}</div>");
            sb.Append($"<div><strong>الوقت:</strong> {transaction.TransactionDate.ToLocalTime():hh:mm tt}".Replace("PM", "م").Replace("AM", "ص") + "</div></div>");
            sb.Append("</div>");

            // Company Info (Left)
            sb.Append("<div class='company-info' style='text-align: left; direction: ltr; display: flex; align-items: center; gap: 15px;'>");
            sb.Append("<img src='/logo.png' alt='Logo' style='max-width: 80px; height: auto; filter: invert(1);' />");
            sb.Append("<div>");
            sb.Append("<h1 style='margin:0; font-size: 24px;'>Draidy - دريدي</h1>");
            sb.Append("<p style='margin:0;'>العنوان هنا</p>");
            sb.Append("<p style='margin:0;'>هاتف: +218 00 0000000</p>");
            sb.Append("</div>");
            sb.Append("</div>");

            sb.Append("</div>");

            // Bill To
            sb.Append("<div class='bill-to'>");
            sb.Append("<h3>بيانات العميل</h3>");
            sb.Append($"<div><strong>الاسم:</strong> {transaction.CustomerName ?? "نقدي"}</div>");
            sb.Append($"<div><strong>الموظف:</strong> {transaction.EmployeeName}</div>");
            sb.Append("</div>");

            // Table
            sb.Append("<table>");
            sb.Append("<thead><tr><th>المنتج</th><th>الكمية</th><th>السعر</th><th>الإجمالي</th></tr></thead>");
            sb.Append("<tbody>");
            foreach (var item in transaction.Items)
            {
                sb.Append("<tr>");
                sb.Append($"<td style='text-align:right'>{item.Product?.Name ?? "Unknown"}</td>");
                sb.Append($"<td>{item.Quantity}</td>");
                sb.Append($"<td>{item.UnitPrice:F2}</td>");
                sb.Append($"<td>{item.Subtotal:F2}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");

            // Totals
            sb.Append("<div class='totals'>");
            
            if (transaction.Discount > 0)
            {
                var subtotal = transaction.TotalAmount + transaction.Discount;
                sb.Append($"<div class='total-row'><span>المجموع الفرعي</span><span>{subtotal:F2}</span></div>");
                sb.Append($"<div class='total-row'><span>الخصم</span><span>{transaction.Discount:F2}</span></div>");
            }
            else 
            {
                sb.Append($"<div class='total-row'><span>المجموع الفرعي</span><span>{transaction.TotalAmount:F2}</span></div>");
            }
            
            sb.Append($"<div class='total-row'><span>المدفوع</span><span>{transaction.TotalPaid:F2}</span></div>");
            if (transaction.BalanceDue > 0.01m)
            {
                sb.Append($"<div class='total-row' style='color:red'><span>المتبقي</span><span>{transaction.BalanceDue:F2}</span></div>");
            }
            
            sb.Append($"<div class='total-row final'><span>الإجمالي</span><span>{transaction.TotalAmount:F2} د.ل</span></div>");
            sb.Append("</div>");

            // Footer
            sb.Append("<div class='footer'>");
            sb.Append("<p>شكراً لتعاملكم معنا</p>");
            sb.Append("</div>");

            sb.Append("</div>"); // End container

            return sb.ToString();
        }

        public string GenerateStatementHtml(string customerName, DateTime? fromDate, DateTime? toDate, List<StatementItem> items)
        {
            // Ensure items are sorted chronologically for the statement
            items = items.OrderBy(i => i.Date).ToList();

            var sb = new StringBuilder();
            // Reuse A4 Styles but adapted for Statement
            sb.Append(@"
<style>
    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; direction: rtl; text-align: right; color: #333; background: #f5f5f5; margin: 0; }
    .a4-container { width: 210mm; min-height: 280mm; padding: 10mm; margin: 20px auto; background: white; box-shadow: 0 0 20px rgba(0,0,0,0.1); box-sizing: border-box; position: relative; }
    
    .header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 30px; border-bottom: 3px solid #34495e; padding-bottom: 15px; }
    .company-info h1 { margin: 0 0 5px 0; color: #2c3e50; font-size: 32px; font-weight: 800; letter-spacing: -1px; }
    .company-info p { margin: 2px 0; color: #666; font-size: 14px; }
    
    .statement-details { text-align: right; }
    .statement-details h2 { margin: 0 0 10px 0; color: #34495e; font-size: 24px; font-weight: 700; }
    .statement-meta { font-size: 14px; color: #555; line-height: 1.5; }
    
    .customer-info { margin-bottom: 30px; background: #f8f9fa; padding: 20px; border-radius: 8px; border-right: 5px solid #34495e; }
    .customer-info h3 { margin-top: 0; margin-bottom: 10px; font-size: 18px; color: #2c3e50; }
    
    table { width: 100%; border-collapse: collapse; margin-bottom: 30px; table-layout: fixed; }
    th { background: #34495e; color: white; padding: 12px; font-size: 14px; text-align: center; font-weight: 600; border: 1px solid #34495e; }
    td { padding: 10px; border: 1px solid #ddd; font-size: 14px; text-align: center; color: #444; }
    tr:nth-child(even) { background-color: #fcfcfc; }
    
    /* Column Widths */
    th:nth-child(1) { width: 20%; } /* Date */
    th:nth-child(2) { width: 40%; text-align: right; padding-right: 15px; } /* Description */
    th:nth-child(3) { width: 20%; } /* Amount */
    th:nth-child(4) { width: 20%; } /* Balance */
    
    td:nth-child(2) { text-align: right; padding-right: 15px; }
    
    .footer { position: absolute; bottom: 0; left: 10mm; right: 10mm; text-align: center; color: #95a5a6; font-size: 13px; border-top: 1px solid #eee; padding-top: 15px; padding-bottom: 10mm; }
    
    .amount-pos { color: green; }
    .amount-neg { color: red; }

    @media print { 
        @page { size: A4; margin: 0; }
        body { margin: 0; padding: 0; background: #fff; } 
        .a4-container { width: auto; max-width: 100%; border: none; margin: 0; padding: 10mm; box-shadow: none; min-height: 260mm; overflow: hidden; page-break-inside: avoid; } 
        .customer-info { background: none; border: 1px solid #eee; border-right: 5px solid #34495e; }
        th { background-color: #34495e !important; color: #fff !important; -webkit-print-color-adjust: exact; }
    }
</style>");

            sb.Append("<div class='a4-container'>");
            
            // Header
            sb.Append("<div class='header'>");

            // Statement Details (Right)
            sb.Append("<div class='statement-details'>");
            sb.Append("<h2>كشف حساب</h2>");
            sb.Append($"<div class='statement-meta'><div><strong>تاريخ الطباعة:</strong> {DateTime.Now:yyyy/MM/dd}</div>");
            if (fromDate.HasValue || toDate.HasValue)
            {
                var fromStr = fromDate.HasValue ? fromDate.Value.ToString("yyyy/MM/dd") : "البداية";
                var toStr = toDate.HasValue ? toDate.Value.ToString("yyyy/MM/dd") : "الآن";
                sb.Append($"<div><strong>الفترة:</strong> {fromStr} - {toStr}</div>");
            }
            sb.Append("</div>");
            sb.Append("</div>");

            // Company Info (Left)
            sb.Append("<div class='company-info' style='text-align: left; direction: ltr; display: flex; align-items: center; gap: 15px;'>");
            sb.Append("<img src='/logo.png' alt='Logo' style='max-width: 80px; height: auto; filter: invert(1);' />");
            sb.Append("<div>");
            sb.Append("<h1 style='margin:0; font-size: 24px;'>Draidy - دريدي</h1>");
            sb.Append("<p style='margin:0;'>كشف حساب عميل</p>");
            sb.Append("</div>");
            sb.Append("</div>");

            sb.Append("</div>");

            // Customer Info
            sb.Append("<div class='customer-info'>");
            sb.Append($"<h3>العميل: {customerName}</h3>");
            sb.Append("</div>");

            // Table
            sb.Append("<table>");
            sb.Append("<thead><tr><th>التاريخ</th><th>البيان</th><th>المبلغ</th><th>الرصيد</th></tr></thead>");
            sb.Append("<tbody>");
            
            foreach (var item in items)
            {
                var amountClass = item.Amount >= 0 ? "amount-pos" : "amount-neg";
                sb.Append("<tr>");
                sb.Append($"<td>{item.Date.ToLocalTime():yyyy/MM/dd hh:mm tt}".Replace("PM", "م").Replace("AM", "ص") + "</td>");
                sb.Append($"<td>{item.Description}</td>");
                sb.Append($"<td class='{amountClass}' dir='ltr'>{item.Amount:N2}</td>");
                sb.Append($"<td dir='ltr'><strong>{item.RunningBalance:N2}</strong></td>");
                sb.Append("</tr>");
            }
            
            sb.Append("</tbody></table>");

            // Final Balance
            var finalBalance = items.LastOrDefault()?.RunningBalance ?? 0;
            sb.Append($"<div style='text-align: left; font-size: 18px; font-weight: bold; margin-top: 20px;'>");
            sb.Append($"الرصيد النهائي: <span dir='ltr'>{finalBalance:N2} LD</span>");
            sb.Append("</div>");

            // Footer
            sb.Append("<div class='footer'>");
            sb.Append("<p>تم استخراج هذا الكشف من النظام الآلي</p>");
            sb.Append("</div>");

            sb.Append("</div>"); // End container

            return sb.ToString();
        }
    }
}
