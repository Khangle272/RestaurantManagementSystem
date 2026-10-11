(() => {
    const form = document.getElementById('dish-form');
    if (!form) return;

    const antiForgeryToken = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";

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

    // --- RE-INDEX SIZES & MULTI-LEVEL INGREDIENT ITEMS ---
    function indexSizes() {
        const container = document.getElementById('sizes-container');
        if (!container) return;

        const cards = container.querySelectorAll('.size-card');
        cards.forEach((card, sIdx) => {
            card.dataset.sizeIndex = sIdx;

            // Re-index size header fields
            card.querySelectorAll('.card-header [data-field]').forEach(input => {
                input.name = `Sizes[${sIdx}].${input.dataset.field}`;
                input.removeAttribute('id');
            });

            // Re-index nested recipes inside this size
            const recipeBody = card.querySelector('.size-recipes-body');
            if (recipeBody) {
                recipeBody.dataset.prefix = `Sizes[${sIdx}].DinhMucItems`;
                recipeBody.querySelectorAll('tr').forEach((row, rIdx) => {
                    row.querySelectorAll('[data-field]').forEach(input => {
                        input.name = `Sizes[${sIdx}].DinhMucItems[${rIdx}].${input.dataset.field}`;
                        input.removeAttribute('id');
                    });
                });
            }
        });
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

    // --- SIZE ACTIONS ---
    const btnAddSize = document.getElementById('btn-add-size');
    const sizeTpl = document.getElementById('size-card-template');
    const recipeRowTpl = document.getElementById('recipe-row-template');

    if (btnAddSize && sizeTpl) {
        btnAddSize.addEventListener('click', () => {
            const container = document.getElementById('sizes-container');
            const newIndex = container.querySelectorAll('.size-card').length;
            const clone = sizeTpl.content.cloneNode(true);
            
            // Default size name
            const nameInput = clone.querySelector('.input-size-name');
            if (nameInput) {
                nameInput.value = newIndex === 0 ? 'Mặc định' : `Size ${String.fromCharCode(65 + newIndex)}`;
            }

            container.appendChild(clone);
            indexSizes();

            const lastCard = container.lastElementChild;
            pulseElement(lastCard);
            lastCard.querySelector('.input-size-name')?.focus();
        });
    }

    // Form Event Delegation for Size & Recipe actions
    form.addEventListener('click', (e) => {
        // Remove Size
        const removeSizeBtn = e.target.closest('.btn-remove-size');
        if (removeSizeBtn) {
            const card = removeSizeBtn.closest('.size-card');
            const totalSizes = document.querySelectorAll('#sizes-container .size-card').length;
            if (totalSizes <= 1) {
                showToast('Món ăn cần ít nhất 1 kích cỡ. Không thể xóa kích cỡ duy nhất!', false);
                return;
            }
            if (confirm('Bạn có chắc muốn xóa kích cỡ này cùng các định mức nguyên liệu bên trong?')) {
                card.remove();
                indexSizes();
            }
            return;
        }

        // Add Recipe to a specific Size
        const addRecipeBtn = e.target.closest('.btn-add-recipe');
        if (addRecipeBtn && recipeRowTpl) {
            const card = addRecipeBtn.closest('.size-card');
            const recipeBody = card?.querySelector('.size-recipes-body');
            if (recipeBody) {
                const clone = recipeRowTpl.content.cloneNode(true);
                recipeBody.appendChild(clone);
                indexSizes();
                recipeBody.lastElementChild.querySelector('.select-nguyenlieu')?.focus();
            }
            return;
        }

        // Remove Recipe from a specific Size
        const removeRecipeBtn = e.target.closest('.btn-remove-recipe');
        if (removeRecipeBtn) {
            const row = removeRecipeBtn.closest('tr');
            row?.remove();
            indexSizes();
            return;
        }

        // Copy Recipe between Sizes trigger
        const copyRecipeBtn = e.target.closest('.btn-copy-recipe');
        if (copyRecipeBtn) {
            openCopyRecipeModal(copyRecipeBtn.closest('.size-card'));
            return;
        }

        // Standard remove button (Combo items)
        const stdRemoveBtn = e.target.closest('[data-remove]');
        if (stdRemoveBtn) {
            const body = stdRemoveBtn.closest('tbody');
            stdRemoveBtn.closest('tr').remove();
            if (body) indexRows(body);
            return;
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
        indexSizes();
        const combosBody = document.getElementById('combos');
        if (combosBody) indexRows(combosBody);
    });

    // --- COPY RECIPES MODAL LOGIC ---
    let currentTargetSizeCard = null;
    const modalCopyEl = document.getElementById('modalCopyRecipe');
    const selectSourceSize = document.getElementById('copySourceSizeSelect');
    const inputCopyRatio = document.getElementById('copyRatioInput');
    const btnExecuteCopy = document.getElementById('btnExecuteCopyRecipe');

    function openCopyRecipeModal(targetCard) {
        currentTargetSizeCard = targetCard;
        const currentIdx = parseInt(targetCard.dataset.sizeIndex, 10);
        const allCards = document.querySelectorAll('#sizes-container .size-card');

        selectSourceSize.innerHTML = '';
        let availableSources = 0;

        allCards.forEach(c => {
            const idx = parseInt(c.dataset.sizeIndex, 10);
            if (idx === currentIdx) return;

            const name = c.querySelector('.input-size-name')?.value?.trim() || `Size #${idx + 1}`;
            const rowCount = c.querySelectorAll('.size-recipes-body tr').length;
            const opt = document.createElement('option');
            opt.value = idx;
            opt.textContent = `${name} (${rowCount} nguyên liệu)`;
            selectSourceSize.appendChild(opt);
            availableSources++;
        });

        if (availableSources === 0) {
            showToast('Chưa có kích cỡ nào khác để sao chép công thức.', false);
            return;
        }

        inputCopyRatio.value = '1.0';
        if (window.bootstrap && bootstrap.Modal) {
            const modal = bootstrap.Modal.getOrCreateInstance(modalCopyEl);
            modal.show();
        }
    }

    if (btnExecuteCopy) {
        btnExecuteCopy.addEventListener('click', () => {
            if (!currentTargetSizeCard) return;

            const sourceIdx = parseInt(selectSourceSize.value, 10);
            const ratio = parseFloat(inputCopyRatio.value) || 1.0;
            const sourceCard = document.querySelector(`#sizes-container .size-card[data-size-index="${sourceIdx}"]`);

            if (!sourceCard || !recipeRowTpl) return;

            const sourceRows = sourceCard.querySelectorAll('.size-recipes-body tr');
            if (sourceRows.length === 0) {
                showToast('Kích cỡ nguồn chưa có nguyên liệu nào.', false);
                return;
            }

            const targetBody = currentTargetSizeCard.querySelector('.size-recipes-body');
            let addedCount = 0;

            sourceRows.forEach(sRow => {
                const ingId = sRow.querySelector('[data-field="NguyenLieuId"]')?.value;
                const qty = parseFloat(sRow.querySelector('[data-field="SoLuong"]')?.value) || 0;
                if (!ingId) return;

                // Check if target already has this ingredient
                const existingSelect = Array.from(targetBody.querySelectorAll('[data-field="NguyenLieuId"]'))
                    .find(s => s.value === ingId);

                if (existingSelect) {
                    // Update quantity
                    const qtyInput = existingSelect.closest('tr')?.querySelector('[data-field="SoLuong"]');
                    if (qtyInput) qtyInput.value = (qty * ratio).toFixed(4).replace(/\.?0+$/, '');
                } else {
                    // Add new row
                    const clone = recipeRowTpl.content.cloneNode(true);
                    const sel = clone.querySelector('[data-field="NguyenLieuId"]');
                    const qtyInput = clone.querySelector('[data-field="SoLuong"]');
                    if (sel) sel.value = ingId;
                    if (qtyInput) qtyInput.value = (qty * ratio).toFixed(4).replace(/\.?0+$/, '');
                    targetBody.appendChild(clone);
                }
                addedCount++;
            });

            indexSizes();

            if (window.bootstrap && bootstrap.Modal) {
                bootstrap.Modal.getInstance(modalCopyEl)?.hide();
            }

            pulseElement(currentTargetSizeCard);
            showToast(`Đã sao chép ${addedCount} nguyên liệu với tỷ lệ x${ratio}!`);
        });
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
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': antiForgeryToken() },
                    body: JSON.stringify({ tenDanhMuc, moTa })
                });

                const data = await res.json();
                if (data.success) {
                    const cboDanhMuc = document.getElementById('cboDanhMuc') || document.querySelector('select[name="DanhMucId"]');
                    if (cboDanhMuc) {
                        const opt = new Option(data.name, data.id, true, true);
                        cboDanhMuc.add(opt);
                        cboDanhMuc.value = data.id;
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
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': antiForgeryToken() },
                    body: JSON.stringify({ tenNguyenLieu, donViTinh, donGia, dinhMucToiThieu })
                });

                const data = await res.json();
                if (data.success) {
                    const text = `${data.name} (${data.unit})`;

                    // 1. Update all existing selects on the page
                    document.querySelectorAll('select.select-nguyenlieu').forEach(sel => {
                        sel.add(new Option(text, data.id));
                    });

                    // 2. Update the template so future rows get the new ingredient
                    if (recipeRowTpl) {
                        const tplSelect = recipeRowTpl.content.querySelector('select.select-nguyenlieu');
                        if (tplSelect) {
                            tplSelect.add(new Option(text, data.id));
                        }
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

    // Initial indexing on load
    indexSizes();
})();
