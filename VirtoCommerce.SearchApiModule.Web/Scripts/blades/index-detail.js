angular.module('virtoCommerce.searchAPIModule')
.controller('virtoCommerce.searchAPIModule.indexDetailController', ['$scope', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'virtoCommerce.searchAPIModule.searchAPIResources', function ($scope, bladeNavigationService, dialogService, searchAPI) {
    var blade = $scope.blade;
    blade.updatePermission = 'VirtoCommerce.Search:Index:Rebuild';
    //blade.isNew = !blade.data.id;

    blade.initialize = function (data) {
        blade.origEntity = data;
        blade.currentEntity = angular.copy(data);
        blade.isLoading = false;
    };

    function openProgressBlade(data) {
        // show indexing progress
        var newBlade = {
            id: 'indexProgress',
            currentEntity: data,
            parentRefresh: blade.parentRefresh,
            controller: 'virtoCommerce.searchAPIModule.indexProgressController',
            template: 'Modules/$(VirtoCommerce.SearchApi)/Scripts/blades/index-progress.tpl.html'
        };

        if (blade.isCatalog) {
            angular.extend(newBlade, {
                title: 'searchAPI.blades.index-progress.title-catalog',
            });
        } else {
            angular.extend(newBlade, {
                title: 'searchAPI.blades.index-progress.title-item',
            });
        }

        bladeNavigationService.showBlade(newBlade, blade.parentBlade);
    }

    blade.headIcon = 'fa-search';
    
    blade.toolbarCommands = [
        {
            name: blade.isCatalog ? "searchAPI.commands.index-missing" : "searchAPI.commands.index", icon: 'fa fa-recycle',
            executeMethod: function () {
                // searchAPI.buildIndex({ id: blade.currentEntityId, updateOnly: true }, openProgressBlade);
                openProgressBlade({ id: blade.currentEntityId });
            },
            canExecuteMethod: function () { return true; },
            permission: 'VirtoCommerce.Search:Index:Rebuild'
        },
        {
            name: "searchAPI.commands.index-all", icon: 'fa fa-recycle',
            executeMethod: function () {
                dialogService.showConfirmationDialog({
                    id: "confirm",
                    title: "searchAPI.dialogs.index-index.title",
                    message: "searchAPI.dialogs.index-index.message",
                    callback: function (confirmed) {
                        if (confirmed) {
                            searchAPI.buildIndex({ catalogId: blade.currentEntityId }, openProgressBlade);
                        }
                    }
                });
            },
            canExecuteMethod: function () { return true; },
            permission: 'VirtoCommerce.Search:Index:Rebuild'
        }
    ];

    if (!blade.isCatalog) {
        blade.toolbarCommands.splice(1, 1); // remove 'index-all'
    }

    if (blade.isNew) {
        angular.extend(blade, {
            title: 'searchAPI.blades.index-detail.title-new',
            subtitle: 'searchAPI.blades.index-detail.subtitle-new'
        });
    } else if (blade.isCatalog) {
        //angular.extend(blade, {
        //});
    } else {
        //angular.extend(blade, {
        //    title: 'searchAPI.blades.index-detail.title'
        //});
    }

    blade.initialize(blade.data);
}]);