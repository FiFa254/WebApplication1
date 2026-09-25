// Add / remove project rows on the profile form and keep field names indexed (Projects[0], Projects[1], ...).
(() => {
    const projectList = document.getElementById("project-list");
    const addProjectButton = document.getElementById("add-project");
    if (!projectList || !addProjectButton) {
        return;
    }

    const maxProjects = Number(addProjectButton.dataset.max) || 20;

    const projectTemplate = (index) => `
        <div class="project-item" data-project-index="${index}">
            <div class="project-item-header">
                <strong>Project ${index + 1}</strong>
                <button type="button" class="btn btn-outline-secondary btn-sm remove-project">Remove</button>
            </div>

            <div class="row">
                <div class="col-md-6 form-group">
                    <label for="Projects_${index}__ProjectName">Project Name</label>
                    <input class="form-control" placeholder="Project name" type="text" data-val="true" data-val-length="The field Project Name must be a string with a maximum length of 150." data-val-length-max="150" id="Projects_${index}__ProjectName" maxlength="150" name="Projects[${index}].ProjectName" value="" />
                    <span class="text-danger field-validation-valid" data-valmsg-for="Projects[${index}].ProjectName" data-valmsg-replace="true"></span>
                </div>

                <div class="col-md-6 form-group">
                    <label for="Projects_${index}__GithubLink">GitHub Link</label>
                    <input class="form-control" placeholder="https://github.com/user/repo" type="url" data-val="true" data-val-length="The field GitHub Link must be a string with a maximum length of 500." data-val-length-max="500" data-val-url="The GitHub Link field is not a valid fully-qualified http, https, or ftp URL." id="Projects_${index}__GithubLink" maxlength="500" name="Projects[${index}].GithubLink" value="" />
                    <span class="text-danger field-validation-valid" data-valmsg-for="Projects[${index}].GithubLink" data-valmsg-replace="true"></span>
                </div>
            </div>

            <div class="form-group">
                <label for="Projects_${index}__Description">Description</label>
                <textarea class="form-control" placeholder="Project description" data-val="true" data-val-length="The field Description must be a string with a maximum length of 1000." data-val-length-max="1000" id="Projects_${index}__Description" maxlength="1000" name="Projects[${index}].Description"></textarea>
                <span class="text-danger field-validation-valid" data-valmsg-for="Projects[${index}].Description" data-valmsg-replace="true"></span>
            </div>
        </div>`;

    const items = () => [...projectList.querySelectorAll(".project-item")];

    const refreshProjects = () => {
        items().forEach((item, index) => {
            item.dataset.projectIndex = index;
            item.querySelector("strong").textContent = `Project ${index + 1}`;

            item.querySelectorAll("input, textarea").forEach((field) => {
                field.name = field.name.replace(/Projects\[\d+\]/, `Projects[${index}]`);
                field.id = field.id.replace(/Projects_\d+__/, `Projects_${index}__`);
            });

            item.querySelectorAll("label").forEach((label) => {
                label.htmlFor = label.htmlFor.replace(/Projects_\d+__/, `Projects_${index}__`);
            });

            item.querySelectorAll("[data-valmsg-for]").forEach((message) => {
                message.dataset.valmsgFor = message.dataset.valmsgFor.replace(/Projects\[\d+\]/, `Projects[${index}]`);
            });
        });

        addProjectButton.disabled = items().length >= maxProjects;
    };

    const parseValidation = () => {
        if (window.jQuery?.validator?.unobtrusive) {
            const form = projectList.closest("form");
            window.jQuery(form).removeData("validator");
            window.jQuery(form).removeData("unobtrusiveValidation");
            window.jQuery.validator.unobtrusive.parse(form);
        }
    };

    addProjectButton.addEventListener("click", () => {
        if (items().length >= maxProjects) {
            return;
        }

        projectList.insertAdjacentHTML("beforeend", projectTemplate(items().length));
        refreshProjects();
        parseValidation();
    });

    projectList.addEventListener("click", (event) => {
        if (!event.target.classList.contains("remove-project")) {
            return;
        }

        // Removing the last row just clears it: an empty row is ignored on save.
        if (items().length === 1) {
            items()[0].querySelectorAll("input, textarea").forEach((field) => field.value = "");
            return;
        }

        event.target.closest(".project-item").remove();
        refreshProjects();
        parseValidation();
    });

    refreshProjects();
})();
