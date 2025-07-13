class JobViewerApp {
  constructor() {
    // --- STATE ---
    this.allJobs = []; // All jobs from the loaded JSON
    this.addedJobs = []; // Full job objects the user has added
    this.displayedAddedJobs = []; // Jobs visible in the sidebar (after filtering)
    this.currentJobId = null; // Use job ID to track the current job
    this.fontSize = 16; // Adjusted default font size for better readability
    this.resumeData = null; // Store resume data
    // --- HELPERS ---
    this.filterTimer = null;

    // --- INITIALIZATION ---
    this.loadSettings();
    this.bindUI();
  }

  bindUI() {
    // A single place to query all DOM elements
    this.elements = {
      btnLoad: document.getElementById('btnLoad'),
      fileInput: document.getElementById('fileInput'),
      btnDecreaseFont: document.getElementById('btnDecreaseFont'),
      btnIncreaseFont: document.getElementById('btnIncreaseFont'),
      lblFontSize: document.getElementById('lblFontSize'),
      placeholder: document.getElementById('placeholder'),
      jobContent: document.getElementById('jobContent'),
      titleText: document.getElementById('titleText'),
      addedIcon: document.getElementById('addedIcon'),
      lblCompany: document.getElementById('lblCompany'),
      lblSalary: document.getElementById('lblSalary'),
      txtSummary: document.getElementById('txtSummary'),
      keySkillsList: document.getElementById('keySkillsList'),
      essentialQualificationsList: document.getElementById('essentialQualificationsList'),
      essentialTechSkillsList: document.getElementById('essentialTechSkillsList'),
      otherTechSkillsList: document.getElementById('otherTechSkillsList'),
      btnApply: document.getElementById('btnApply'),
      btnPrev: document.getElementById('btnPrev'),
      btnNext: document.getElementById('btnNext'),
      progressBar: document.getElementById('progressBar'),
      progressText: document.getElementById('progressText'),
      btnAddJob: document.getElementById('btnAddJob'),
      btnDeleteJob: document.getElementById('btnDeleteJob'),
      btnExportJson: document.getElementById('btnExportJson'),
      addedJobList: document.getElementById('addedJobList'),
      txtSearch: document.getElementById('txtSearch'),
      btnLoadResume: document.getElementById('btnLoadResume'),
      resumeInput: document.getElementById('resumeInput'),
      rawDescriptionContent: document.getElementById('rawDescriptionContent')
    };

    // Bind all event listeners
    this.elements.btnLoad.addEventListener('click', () => this.elements.fileInput.click());
    this.elements.fileInput.addEventListener('change', (e) => this.handleFileChange(e));
    this.elements.btnDecreaseFont.addEventListener('click', () => this.changeFontSize(-1));
    this.elements.btnIncreaseFont.addEventListener('click', () => this.changeFontSize(1));
    this.elements.btnPrev.addEventListener('click', () => this.navigateJobs(-1));
    this.elements.btnNext.addEventListener('click', () => this.navigateJobs(1));
    this.elements.btnAddJob.addEventListener('click', () => this.addCurrentJob());
    this.elements.btnDeleteJob.addEventListener('click', () => this.deleteCurrentJob());
    this.elements.btnExportJson.addEventListener('click', () => this.exportJson());
    this.elements.txtSearch.addEventListener('input', () => this.filterAddedJobs());
    this.elements.btnLoadResume.addEventListener('click', () => this.elements.resumeInput.click());
    this.elements.resumeInput.addEventListener('change', (e) => this.handleResumeChange(e));
    this.elements.detailsRaw = document.getElementById('jobRawDescription');
    this.elements.detailsRaw.addEventListener('toggle', () => { console.log(`Raw job description is now ${this.elements.detailsRaw.open ? 'open' : 'closed'}`); });


    // Initial UI state
    this.elements.btnAddJob.classList.add('hidden');
    this.elements.btnDeleteJob.classList.add('hidden');
    this.elements.addedIcon.classList.add('hidden');
    this.updateFonts();
  }

  handleResumeChange(e) {
    const file = e.target.files[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = event => {
      try {
        this.resumeData = JSON.parse(event.target.result);
        console.log('Resume loaded successfully');

        // Update display if we have a job loaded
        if (this.currentJobId) {
          this.updateDisplay();
        }
      } catch (error) {
        alert(`Failed to load resume: ${error.message}`);
      }
    };
    reader.onerror = () => alert(`Error reading file: ${reader.error}`);
    reader.readAsText(file);
  }
  handleFileChange(e) {
    const file = e.target.files[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = event => {
      try {
        const data = JSON.parse(event.target.result);
        if (!Array.isArray(data) || (data.length > 0 && typeof data[0].Id === 'undefined')) {
          throw new Error("JSON must be an array of objects, and each object must have a unique 'Id' property.");
        }

        this.allJobs = data;
        this.currentJobId = data.length > 0 ? data[0].Id : null;

        if (this.currentJobId !== null) {
          this.elements.placeholder.classList.add('hidden');
          this.elements.jobContent.classList.remove('hidden');
        } else {
          this.elements.placeholder.classList.remove('hidden');
          this.elements.jobContent.classList.add('hidden');
        }

        this.updateDisplay();
        console.log(`Loaded ${data.length} jobs from ${file.name}`);
      } catch (error) {
        alert(`Failed to load file: ${error.message}`);
      }
    };
    reader.onerror = () => alert(`Error reading file: ${reader.error}`);
    reader.readAsText(file);
  }

  updateAddedJobList() {
    const ul = this.elements.addedJobList;
    ul.innerHTML = ""; // Clear existing list
    this.displayedAddedJobs.forEach((job, index) => {
      const li = document.createElement("li");
      li.textContent = `${index + 1}. ${job['Job Offer Title'] || 'Untitled'} - ${job['Company Name'] || 'Unknown'}`;
      li.dataset.Id = job.Id; // Store job ID for quick lookup
      li.addEventListener('click', () => this.jumpToJob(job.Id));
      ul.appendChild(li);
    });
  }

  filterAddedJobs() {
    const searchTerm = this.elements.txtSearch.value.toLowerCase();
    this.displayedAddedJobs = this.addedJobs.filter(job =>
      (job['Job Offer Title'] || '').toLowerCase().includes(searchTerm) ||
      (job['Company Name'] || '').toLowerCase().includes(searchTerm)
    );
    this.updateAddedJobList();
  }

  jumpToJob(jobId) {
    const jobIndex = this.allJobs.findIndex(job => job.Id === jobId);
    if (jobIndex !== -1) {
      this.currentJobId = jobId;
      this.updateDisplay();
    }
  }

  showButtons(isAdded) {
    this.elements.addedIcon.classList.toggle('hidden', !isAdded);
    this.elements.btnDeleteJob.classList.toggle('hidden', !isAdded);
    this.elements.btnAddJob.classList.toggle('hidden', isAdded);
  }

  populateList(element, items, tagClass) {
    element.innerHTML = '';
    if (Array.isArray(items) && items.length > 0) {
      items.forEach(item => {
        const li = document.createElement('li');
        if (tagClass) li.className = tagClass;
        if (typeof item === 'string') {
          li.textContent = item;
        } else if (typeof item === 'object' && item.Name) {
          li.textContent = `${item.Name}${item.RelevancePercentage ? ` (${item.RelevancePercentage}%)` : ''}`;
        }
        element.appendChild(li);
      });
    }
  }

  updateDisplay() {
    console.log('Write updateDisplay');
    if (!this.currentJobId || this.allJobs.length === 0) {
      this.elements.progressText.textContent = `0 / 0`;
      this.elements.progressBar.value = 0;
      console.log('Job is found');
      this.toggleNavigation(false, -1);
      return;
    }

    const currentIndex = this.allJobs.findIndex(j => j.Id === this.currentJobId);
    if (currentIndex === -1) {
      console.error("Could not find job with ID:", this.currentJobId);
      this.currentJobId = this.allJobs.length > 0 ? this.allJobs[0].Id : null;
      this.updateDisplay();
      return;
    }
    const job = this.allJobs[currentIndex];

    this.elements.titleText.textContent = job.JobOfferTitle || 'No title';
    this.elements.lblCompany.textContent = job.CompanyName || 'Not specified';
    this.elements.lblSalary.textContent = job.SalaryOrBudgetOffered || 'Not specified';
    this.elements.txtSummary.textContent = job.JobOfferSummarize || 'No summary available.';

    if (Array.isArray(job.RawJobDescription)) {
      this.elements.rawDescriptionContent.innerHTML = job.RawJobDescription
        .map(line => `<p>${line}</p>`)
        .join('');
      console.log('rawDescriptionContent successfully');
    } else {
      this.elements.rawDescriptionContent.innerHTML = '<p>No additional description available.</p>';
      console.log('rawDescriptionContent faiuled');
    }

    this.populateList(this.elements.keySkillsList, job.KeySkillsRequired, 'skill-tag');
    this.populateList(this.elements.essentialQualificationsList, job.EssentialQualifications);
    this.populateList(this.elements.essentialTechSkillsList, job.EssentialTechnicalSkillQualifications);
    this.populateList(this.elements.otherTechSkillsList, job.OtherTechnicalSkillQualifications);


    const isAdded = this.addedJobs.some(j => j.Id === this.currentJobId);
    const hasLink = job.Url && job.Url.trim() !== '';
    this.elements.btnApply.classList.toggle('hidden', !hasLink);
    if (hasLink) this.elements.btnApply.href = job.Url;

    this.elements.progressBar.max = this.allJobs.length;
    this.elements.progressBar.value = currentIndex + 1;
    this.elements.progressText.textContent = `${currentIndex + 1} / ${this.allJobs.length}`;
    document.title = `Job Viewer (${currentIndex + 1}/${this.allJobs.length})`;

    this.toggleNavigation(true, currentIndex);
    this.showButtons(isAdded);

    // Update match percentage if resume is loaded

    if (this.resumeData) {
    console.log('Resume ready, calculating match percentage...');
    const percentage = this.calculateMatchPercentage(job);
    this.updateGauge(percentage, '.gauge-arc', '.gauge-text');
    }
  }

  calculateMatchPercentage(job) {
    if (!this.resumeData || !Array.isArray(job.Skills)) return 0;
    console.log('Calculate match percentage by skills...', job.Skills);
    let total = 0;
    job.Skills.forEach(skill => {
      const resumeScore = this.resumeData[skill.Category] || 0;
      console.log('ResumeScore...', resumeScore);
      total += skill.Relevance * (resumeScore / 100);
    });
    console.log('Calculate match percentage by total...', total);
    let percentage = Math.min(Math.round(total), 100);
    console.log('Calculate match percentage by percentage...', percentage);
    return percentage;
  }

  updateGauge(percentage, arcSelector, textSelector) {
    const circumference = 339.292;
    const offset = circumference - (percentage / 100) * circumference;
    document.querySelector(arcSelector).style.strokeDashoffset = offset;
    document.querySelector(textSelector).textContent = `${percentage}%`;
    console.log(`percentage ${percentage}%`);
  }

  toggleNavigation(enabled, currentIndex) {
    this.elements.btnPrev.disabled = !enabled || currentIndex <= 0;
    this.elements.btnNext.disabled = !enabled || currentIndex >= this.allJobs.length - 1;
    this.elements.btnAddJob.disabled = !enabled;
    this.elements.btnDeleteJob.disabled = !enabled;
  }

  navigateJobs(direction) {
    if (!this.currentJobId) return;
    const currentIndex = this.allJobs.findIndex(j => j.Id === this.currentJobId);
    const newIndex = currentIndex + direction;

    if (newIndex >= 0 && newIndex < this.allJobs.length) {
      this.currentJobId = this.allJobs[newIndex].Id;
      this.updateDisplay();
    }
  }

  changeFontSize(delta) {
    const newSize = this.fontSize + delta;
    if (newSize >= 12 && newSize <= 28) {
      this.fontSize = newSize;
      this.updateFonts();
    }
  }

  updateFonts() {
    document.documentElement.style.fontSize = `${this.fontSize}px`;
    this.elements.lblFontSize.textContent = `Font: ${this.fontSize}px`
    this.saveSettings();
  }

  loadSettings() {
    const savedSize = localStorage.getItem('jobViewerFontSize');
    if (savedSize) this.fontSize = parseInt(savedSize, 10);
  }

  saveSettings() {
    localStorage.setItem('jobViewerFontSize', this.fontSize.toString());
  }

  addCurrentJob() {
    if (!this.currentJobId) return;

    const currentJob = this.allJobs.find(j => j.Id === this.currentJobId);
    if (!currentJob) return;

    const isAlreadyAdded = this.addedJobs.some(job => job.Id === this.currentJobId);
    if (!isAlreadyAdded) {
      this.addedJobs.push(currentJob);
      this.filterAddedJobs();
      this.updateDisplay();
    }
  }

  deleteCurrentJob() {
    if (!this.currentJobId) return;
    this.addedJobs = this.addedJobs.filter(job => job.Id !== this.currentJobId);
    this.filterAddedJobs();
    this.updateDisplay();

  }

  exportJson() {
    if (this.addedJobs.length === 0) {
      alert("No added jobs to export.");
      return;
    }
    const dataStr = JSON.stringify(this.addedJobs, null, 2);
    const blob = new Blob([dataStr], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = "selected_jobs.json";
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
}

document.addEventListener('DOMContentLoaded', () => { new JobViewerApp(); });