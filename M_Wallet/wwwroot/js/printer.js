window.printer = {
    printHtml: (htmlContent) => {
        let iframe = document.getElementById('printFrame');
        if (!iframe) {
            iframe = document.createElement('iframe');
            iframe.id = 'printFrame';
            iframe.style.position = 'absolute';
            iframe.style.width = '0px';
            iframe.style.height = '0px';
            iframe.style.border = 'none';
            document.body.appendChild(iframe);
        }
        
        const doc = iframe.contentWindow.document;
        doc.open();
        doc.write('<!DOCTYPE html><html lang="ar" dir="rtl"><head><title> </title>');
        doc.write('</head><body style="margin:0; padding:0;">');
        doc.write(htmlContent);
        doc.write('</body></html>');
        doc.close();

        setTimeout(() => {
            iframe.contentWindow.focus();
            iframe.contentWindow.print();
        }, 500);
    }
};
