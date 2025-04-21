var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $("#tblData").DataTable({ //the id provided must match the tag of the html table id set in the view, in this case "tblData"
        "ajax": { url: "/admin/company/getall" }, //api endpoint to get the data to be used to feed this table
        "columns": [ //define the value for each column in the view. The number of th you define in the table structure must match the number of columns we define here
            { data: "name", "width": "15%" },
            { data: "streetAddress", "width": "15%" },
            { data: "city", "width": "15%" },
            { data: "state", "width": "15%" },
            { data: "phoneNumber", "width": "15%" },
            {
                data: "id",
                "render": function(data){ //render is a specifial function to render html as the value of this column, data parameter received in this case is the "id"
                    return `
                        <div class="w-75 btn-group" role="group">
                            <a href="/admin/company/upsert?id=${data}" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i> Edit</a>
                            <a onClick="Delete('/admin/company/delete/${data}')" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i> Delete</a>
                        </div>
                    `;
                },
                "width": "25%"
            }
        ]
    });
}

function Delete(url){
    Swal.fire({
        title: "Are you sure?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url, //url provided to delete the data
                type: 'DELETE', //must be type delete, as we set it in our controller, cannot be post nor get
                success: function (data) {
                    dataTable.ajax.reload(); //reload the page using datatable, so the changes are reflected on the table
                    toastr.success(data.message); //display a success message coming from the api as json
                }
            })
        }
    });
}