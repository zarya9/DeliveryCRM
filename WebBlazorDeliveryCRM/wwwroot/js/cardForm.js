window.dmsCardForm = {
    read: function (numberEl, expiryEl, cvvEl, holderEl) {
        var val = function (el) {
            return el && typeof el.value === 'string' ? el.value : '';
        };
        return {
            number: val(numberEl),
            expiry: val(expiryEl),
            cvv: val(cvvEl),
            holder: val(holderEl)
        };
    }
};
