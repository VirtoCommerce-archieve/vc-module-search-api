angular.module('virtoCommerce.searchAPIModule')
.controller('virtoCommerce.searchAPIModule.indexWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.searchAPIModule.searchAPIResources', function ($scope, bladeNavigationService, searchAPI) {
    var blade = $scope.blade;
    $scope.loading = true;

    searchAPI.get({ documentType: 'item', documentId: blade.itemId }, function (data) {
        // _.each([{ id: blade.itemId, buildDate: new Date(), content:'function updateStatus() {    \n    if ($scope.index && blade.currentEntity) {        $scope.loading = false;        if (!$scope.index.id)' }], function (data) {
        $scope.index = data;
        updateStatus();
    });

    $scope.$watch('blade.currentEntity', updateStatus);

    function updateStatus() {
        if ($scope.index && blade.currentEntity) {
            $scope.loading = false;
            if (!$scope.index.id) {
                $scope.widget.UIclass = 'error';
            } else if ($scope.index.buildDate < blade.currentEntity.modifiedDate)
                $scope.widget.UIclass = 'error';
        }
    }

    $scope.openBlade = function () {
        var newBlade = {
            id: 'itemDetailChild',
            currentEntityId: blade.itemId,
            data: $scope.index,
            parentRefresh: blade.parentRefresh,
            title: 'searchAPI.blades.index-detail.title',
            controller: 'virtoCommerce.searchAPIModule.indexDetailController',
            template: 'Modules/$(VirtoCommerce.SearchApi)/Scripts/blades/index-detail.tpl.html'
        };
        bladeNavigationService.showBlade(newBlade, blade);
    };
}]);
