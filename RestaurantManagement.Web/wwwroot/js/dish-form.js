(() => {
    const form = document.getElementById('dish-form');
    if (!form) return;

    function indexRows(body) {
        body.querySelectorAll('tr').forEach((row, index) => {
            row.querySelectorAll('[data-field]').forEach(input => {
                input.name = body.dataset.prefix + '[' + index + '].' + input.dataset.field;
                input.removeAttribute('id');
            });
        });
    }

    document.querySelectorAll('[data-add]').forEach(button => {
        button.addEventListener('click', () => {
            const body = document.getElementById(button.dataset.add);
            body.append(document.getElementById(button.dataset.template).content.cloneNode(true));
            indexRows(body);
        });
    });

    form.addEventListener('click', event => {
        const button = event.target.closest('[data-remove]');
        if (!button) return;
        const body = button.closest('tbody');
        button.closest('tr').remove();
        indexRows(body);
    });

    form.addEventListener('submit', () => form.querySelectorAll('tbody[data-prefix]').forEach(indexRows));

    // Toast helper
    function showToast(message, isSuccess = true) {
        const toastEl = document.getElementById('quickToast');
        const toastMsg = document.getElementById('quickToastMessage');
        if (!toastEl || !toastMsg) return;

        toastMsg.textContent = message;
        toastEl.className = `toast align-items-center text-white border-0 shadow ${isSuccess ? 'bg-success' : 'bg-danger'}`;
        if (window.bootstrap && bootstrap.Toast) {
            const toast = new bootstrap.Toast(toastEl, { delay: 3500 });
            toast.show();
        } else {
            alert(message);
        }
    }

    // Flash visual feedback on element
    function pulseElement(el) {
        if (!el) return;
        el.classList.add('pulse-green');
        setTimeout(() => el.classList.remove('pulse-green'), 1500);
    }

    // --- QUICK ADD CATEGORY ---
    const formQuickCategory = document.getElementById('formQuickAddCategory');
    const modalCategoryEl = document.getElementById('modalQuickAddCategory');
    const categoryAlert = document.getElementById('quickCategoryAlert');
    const btnSaveCategory = document.getElementById('btnSaveQuickCategory');

    if (formQuickCategory) {
        formQuickCategory.addEventListener('submit', async (e) => {
            e.preventDefault();
            const tenDanhMuc = document.getElementById('quickTenDanhMuc')?.value?.trim();
            const moTa = document.getElementById('quickMoTaDanhMuc')?.value?.trim();

            if (!tenDanhMuc) {
                if (categoryAlert) {
                    categoryAlert.textContent = 'Vui lòng nhập tên danh mục.';
                    categoryAlert.classList.remove('d-none');
                }
                return;
            }

            if (categoryAlert) categoryAlert.classList.add('d-none');
            const originalBtnHtml = btnSaveCategory.innerHTML;
            btnSaveCategory.disabled = true;
            btnSaveCategory.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span> Đang lưu...';

            try {
                const res = await fetch('/DanhMuc/QuickCreate', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ tenDanhMuc, moTa })
                });

                const data = await res.json();
                if (data.success) {
                    // Update main select
                    const cboDanhMuc = document.getElementById('cboDanhMuc') || document.querySelector('select[name="DanhMucId"]');
                    if (cboDanhMuc) {
                        const opt = new Option(data.name, data.id, true, true);
                        cboDanhMuc.add(opt);
                        cboDanhMuc.value = data.id;
                        pulseElement(cboDanhMuc);
                    }

                    // Hide modal & reset form
                    if (window.bootstrap && bootstrap.Modal) {
                        const modal = bootstrap.Modal.getInstance(modalCategoryEl) || new bootstrap.Modal(modalCategoryEl);
                        modal.hide();
                    }
                    formQuickCategory.reset();
                    showToast(`Đã thêm danh mục "${data.name}" thành công!`);
                } else {
                    if (categoryAlert) {
                        categoryAlert.textContent = data.message || 'Lỗi khi lưu danh mục.';
                        categoryAlert.classList.remove('d-none');
                    }
                }
            } catch (err) {
                if (categoryAlert) {
                    categoryAlert.textContent = 'Không thể kết nối đến máy chủ: ' + err.message;
                    categoryAlert.classList.remove('d-none');
                }
            } finally {
                btnSaveCategory.disabled = false;
                btnSaveCategory.innerHTML = originalBtnHtml;
            }
        });
    }

    // --- QUICK ADD INGREDIENT ---
    const formQuickIngredient = document.getElementById('formQuickAddIngredient');
    const modalIngredientEl = document.getElementById('modalQuickAddIngredient');
    const ingredientAlert = document.getElementById('quickIngredientAlert');
    const btnSaveIngredient = document.getElementById('btnSaveQuickIngredient');

    if (formQuickIngredient) {
        formQuickIngredient.addEventListener('submit', async (e) => {
            e.preventDefault();
            const tenNguyenLieu = document.getElementById('quickTenNguyenLieu')?.value?.trim();
            const donViTinh = document.getElementById('quickDonViTinh')?.value?.trim();
            const donGia = parseFloat(document.getElementById('quickDonGia')?.value) || 0;
            const dinhMucToiThieu = parseFloat(document.getElementById('quickDinhMucToiThieu')?.value) || 0;

            if (!tenNguyenLieu || !donViTinh) {
                if (ingredientAlert) {
                    ingredientAlert.textContent = 'Vui lòng nhập tên nguyên liệu và đơn vị tính.';
                    ingredientAlert.classList.remove('d-none');
                }
                return;
            }

            if (ingredientAlert) ingredientAlert.classList.add('d-none');
            const originalBtnHtml = btnSaveIngredient.innerHTML;
            btnSaveIngredient.disabled = true;
            btnSaveIngredient.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span> Đang lưu...';

            try {
                const res = await fetch('/NguyenLieu/QuickCreate', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ tenNguyenLieu, donViTinh, donGia, dinhMucToiThieu })
                });

                const data = await res.json();
                if (data.success) {
                    const text = `${data.name} (${data.unit})`;

                    // 1. Update all existing ingredient selects in #recipes
                    document.querySelectorAll('#recipes select[data-field="NguyenLieuId"]').forEach(sel => {
                        sel.add(new Option(text, data.id));
                    });

                    // 2. Update the template so future rows get the new ingredient
                    const recipeTpl = document.getElementById('recipe-template');
                    if (recipeTpl) {
                        const tplSelect = recipeTpl.content.querySelector('select[data-field="NguyenLieuId"]');
                        if (tplSelect) {
                            tplSelect.add(new Option(text, data.id));
                        }
                    }

                    // 3. Automatically add a new row with this ingredient selected
                    const recipesBody = document.getElementById('recipes');
                    if (recipesBody && recipeTpl) {
                        const rowNode = recipeTpl.content.cloneNode(true);
                        const sel = rowNode.querySelector('select[data-field="NguyenLieuId"]');
                        if (sel) sel.value = data.id;
                        recipesBody.append(rowNode);
                        indexRows(recipesBody);
                        pulseElement(recipesBody.lastElementChild);
                    }

                    // Hide modal & reset form
                    if (window.bootstrap && bootstrap.Modal) {
                        const modal = bootstrap.Modal.getInstance(modalIngredientEl) || new bootstrap.Modal(modalIngredientEl);
                        modal.hide();
                    }
                    formQuickIngredient.reset();
                    showToast(`Đã thêm nguyên liệu "${data.name}" vào kho thành công!`);
                } else {
                    if (ingredientAlert) {
                        ingredientAlert.textContent = data.message || 'Lỗi khi lưu nguyên liệu.';
                        ingredientAlert.classList.remove('d-none');
                    }
                }
            } catch (err) {
                if (ingredientAlert) {
                    ingredientAlert.textContent = 'Không thể kết nối đến máy chủ: ' + err.message;
                    ingredientAlert.classList.remove('d-none');
                }
            } finally {
                btnSaveIngredient.disabled = false;
                btnSaveIngredient.innerHTML = originalBtnHtml;
            }
        });
    }
})();
