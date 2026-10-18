(() => {
    const form = document.getElementById('dish-form');
    if (!form) return;

    const antiForgeryToken = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";

    // POST JSON có thử lại 1 lần: sau khi restart server, tab cũ có thể giữ kết nối đã đóng
    // khiến fetch ném "Failed to fetch" dù server đã chạy lại.
    async function postJson(url, payload, retries = 1) {
        try {
            return await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': antiForgeryToken() },
                body: JSON.stringify(payload)
            });
        } catch (error) {
            if (retries <= 0) throw error;
            await new Promise(resolve => setTimeout(resolve, 400));
            return postJson(url, payload, retries - 1);
        }
    }

    function networkErrorText(error) {
        return 'Không thể kết nối đến máy chủ. Kiểm tra ứng dụng đang chạy, tải lại trang (Ctrl+F5) rồi thử lại. Chi tiết: ' + error.message;
    }

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

    // Re-index standard tables (Combos)
    function indexRows(body) {
        if (!body) return;
        body.querySelectorAll('tr').forEach((row, index) => {
            row.querySelectorAll('[data-field]').forEach(input => {
                input.name = body.dataset.prefix + '[' + index + '].' + input.dataset.field;
                input.removeAttribute('id');
            });
        });
    }

    // Form Event Delegation for Combo items
    form.addEventListener('click', (e) => {
        const stdRemoveBtn = e.target.closest('[data-remove]');
        if (stdRemoveBtn) {
            const body = stdRemoveBtn.closest('tbody');
            stdRemoveBtn.closest('tr').remove();
            if (body) indexRows(body);
        }
    });

    // Combos add button
    document.querySelectorAll('[data-add]').forEach(button => {
        button.addEventListener('click', () => {
            const body = document.getElementById(button.dataset.add);
            const tpl = document.getElementById(button.dataset.template);
            if (body && tpl) {
                body.append(tpl.content.cloneNode(true));
                indexRows(body);
            }
        });
    });

    // Form submit validation & indexing
    form.addEventListener('submit', () => {
        const combosBody = document.getElementById('combos');
        if (combosBody) indexRows(combosBody);
    });

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
                const res = await postJson('/DanhMuc/QuickCreate', { tenDanhMuc, moTa });

                const data = await res.json();
                if (data.success) {
                    const cboDanhMuc = document.getElementById('cboDanhMuc') || document.querySelector('select[name="DanhMucId"]');
                    if (cboDanhMuc) {
                        const opt = new Option(data.name, data.id, true, true);
                        opt.dataset.thucUong = 'false';
                        cboDanhMuc.add(opt);
                        cboDanhMuc.value = data.id;
                        cboDanhMuc.dispatchEvent(new Event('change'));
                        pulseElement(cboDanhMuc);
                    }

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
                    categoryAlert.textContent = networkErrorText(err);
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
                const res = await postJson('/NguyenLieu/QuickCreate', { tenNguyenLieu, donViTinh, donGia, dinhMucToiThieu });

                const data = await res.json();
                if (data.success) {
                    // Bổ sung lựa chọn cho mọi dòng đang có và các dòng tạo mới sau này.
                    window.dishBom?.addIngredient({ Id: data.id, TenNguyenLieu: data.name, DonViTinh: data.unit, DonGia: donGia });

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
                    ingredientAlert.textContent = networkErrorText(err);
                    ingredientAlert.classList.remove('d-none');
                }
            } finally {
                btnSaveIngredient.disabled = false;
                btnSaveIngredient.innerHTML = originalBtnHtml;
            }
        });
    }

    // Toggle combo section visibility based on dish type
    const selectLoaiMon = document.getElementById('selectLoaiMon');
    const comboSection = document.getElementById('combo-section');
    if (selectLoaiMon && comboSection) {
        function updateComboVisibility() {
            comboSection.style.display = selectLoaiMon.value === 'Set' ? 'block' : 'none';
        }
        selectLoaiMon.addEventListener('change', updateComboVisibility);
        updateComboVisibility();
    }
})();
